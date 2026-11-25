# m5_alpha_release: testing

- [Back to index](index.md)
- [M5 build and run](../building/m5_alpha_release_build.md)
- [M5 implementation](../coding/m5_alpha_release_implementation.md)
- [M5 deployment](../deployment/m5_alpha_release_deployment.md)
- [M5 operations](../operations/m5_alpha_release_operations.md)
- [M4 security and multi-tenancy build](../building/m4_security_multi_tenancy_build.md)
- [Token issuer](../../tools/Mtpr.LocalTokenService/JwtTokenIssuer.cs)
- [JWT validation tests](../../tests/Mtpr.IntegrationTests/Security/JwtValidations/JwtValidationTests.cs)
- [Helpdesk sample tests](../../tests/Mtpr.IntegrationTests/HelpdeskSample/HelpdeskSampleTests.cs)

The token service has no automated test project yet. The M4 integration tests validate the API against a simulated identity provider and remain the automated coverage for token validation. The service itself is verified manually. The helpdesk sample is covered by integration tests that load the documented JSON files.

## Build and regression check

```powershell
dotnet build MTPR.slnx -warnaserror
dotnet test MTPR.slnx
```

## Sample application check

```powershell
dotnet test tests/Mtpr.IntegrationTests --filter "FullyQualifiedName~HelpdeskSample"
```

## Token service manual checks

| Check | Expected result |
| --- | --- |
| `POST /token` with valid client credentials | HTTP 200 with a Bearer token and `expires_in` of `TokenLifetimeMinutes * 60`. |
| Wrong secret or unknown client | HTTP 401, `invalid_client`. |
| Another `grant_type` | HTTP 400, `unsupported_grant_type`. |
| `GET /.well-known/jwks.json` | One RS256 key whose `kid` equals the token header `kid`. |
| Restart with the same key file | Same `kid`. |
| Restart with `LocalTokenService__SigningKeyPem` set | Different `kid` from the file key. |
| Invalid PEM in `SigningKeyPem` | Startup or first-use failure; the file key is not used. |
| API call with the issued token (matching Authority and Audience) | HTTP 200 for a permitted role; HTTP 403 without a matching grant. |

Last full run on the solution: 279 unit, 9 test-utility and 139 integration tests passed.

## Helpdesk end-to-end script

The script in this document exercises the running helpdesk sample over HTTP with `curl.exe`. Run it in PowerShell with the token service and the API running as described in the build document, with the `editor` role granted `create`, `read`, `update` and `delete` on `customer`, `ticket` and `ticketcomment`. Each run creates unique data, so it can be repeated. The exit code is 0 when every check passes and 1 otherwise.

The secret parameters default to the placeholder values of the token service settings. Pass the real values of your environment and never commit them.

### Manual testing of the same flow

Use these steps to check a single behavior by hand or to diagnose a failing script check. Replace `$api` with the API address.

```powershell
$api = 'http://127.0.0.1:5080'
$token = (Invoke-RestMethod -Method Post -Uri 'https://localhost:5443/token' -Body @{
    grant_type    = 'client_credentials'
    client_id     = 'editor-client'
    client_secret = '<editor secret>'
}).access_token
$h = @{ Authorization = "Bearer $token" }
```

The header must be `Bearer <token>`. Using the `reader-client` token without a `reader` role grant gives HTTP 403.

| Step | Request | Expected result |
| --- | --- | --- |
| Create a customer | `POST /api/customer` with `name` and `email` | HTTP 201 with an `id`. |
| Duplicate email | Repeat the same `POST` | HTTP 409 or 422. |
| Create a ticket | `POST /api/ticket` with `subject` and the customer id | HTTP 201, `status` `open`, `priority` 2, `cost` 40. |
| Business rule | `POST /api/ticket` with `hours` 41 | HTTP 422, code `MTPR-2011`; `hours` 40 is accepted. |
| Computed field | `PATCH` the ticket with `hours` 2 | `cost` is recomputed; writing `cost` itself is refused. |
| Lifecycle | `PATCH` `status` along `open`, `inProgress`, `resolved`, `closed` | Each step succeeds; `open` to `closed` gives 422, `MTPR-2012`. |
| Comments | `POST /api/ticketcomment` with the ticket id and `body` | HTTP 201; an empty body gives 422. |
| Querying | `GET /api/ticket?filter=status:eq:open&sort=-priority&take=5&fields=subject,status` | Filtered, sorted and projected page. |
| Bad filter | `GET /api/ticket?filter=nope:eq:1` | HTTP 400, code `MTPR-2010`. |
| Restrict delete | `DELETE /api/customer/{id}` while it has tickets | HTTP 409. |
| Cascade delete | `DELETE /api/ticket/{id}` | HTTP 204; its comments are gone. |

Example of one manual call and reading the error body:

```powershell
try {
    Invoke-RestMethod -Method Post -Uri "$api/api/ticket" -Headers $h -ContentType 'application/json' `
        -Body '{ "subject": "Too long", "customerId": "<customer id>", "hours": 41 }'
} catch {
    (New-Object IO.StreamReader $_.Exception.Response.GetResponseStream()).ReadToEnd()
}
```

Last run of the script: 56 checks passed, 0 failed.

### Script source

```powershell
<#
.SYNOPSIS
    End-to-end test of the M5 helpdesk sample using curl.exe.

.DESCRIPTION
    Requires the token service and the API running as described in
    docs/building/m5_alpha_release_build.md, with the editor role granted
    create/read/update/delete on customer, ticket and ticketcomment:

      $env:Mtpr__Authorization__Roles__editor__customer__0 = 'create'  (..__1 read, __2 update, __3 delete)
      $env:Mtpr__Authorization__Roles__editor__ticket__0 = 'create'    (..__1 read, __2 update, __3 delete)
      $env:Mtpr__Authorization__Roles__editor__ticketcomment__0 = 'create' (..__1 read, __2 update, __3 delete)

    The script uses unique data on each run, so it can be repeated.
    Exit code is 0 when all checks pass, 1 otherwise.
#>
[CmdletBinding()]
param(
    [string] $Api = 'http://127.0.0.1:5080',
    [string] $TokenUrl = 'https://localhost:5443/token',
    [string] $EditorClient = 'editor-client',
    [string] $EditorSecret = 'editor-secret-change-me',
    [string] $ReaderClient = 'reader-client',
    [string] $ReaderSecret = 'reader-secret-change-me'
)

$ErrorActionPreference = 'Stop'
$script:passed = 0
$script:failed = 0
$script:tmp = Join-Path ([IO.Path]::GetTempPath()) ("helpdesk-" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $script:tmp | Out-Null

function Invoke-Curl {
    param([string] $Method, [string] $Url, $Body = $null, [string] $Token = $null, [hashtable] $Form = $null)

    $out = Join-Path $script:tmp 'response.txt'
    $curlArgs = @('-s', '-k', '-o', $out, '-w', '%{http_code}', '-X', $Method, $Url)

    if ($Token) { $curlArgs += @('-H', "Authorization: Bearer $Token") }

    if ($Form) {
        foreach ($key in $Form.Keys) { $curlArgs += @('--data-urlencode', "$key=$($Form[$key])") }
    }
    elseif ($null -ne $Body) {
        $in = Join-Path $script:tmp 'request.json'
        [IO.File]::WriteAllText($in, ($Body | ConvertTo-Json -Compress -Depth 5), (New-Object Text.UTF8Encoding $false))
        $curlArgs += @('-H', 'Content-Type: application/json', '--data-binary', "@$in")
    }

    $status = [int](& curl.exe @curlArgs)
    $text = if (Test-Path $out) { Get-Content $out -Raw } else { '' }
    $json = $null
    if ($text) { try { $json = $text | ConvertFrom-Json } catch { } }

    [pscustomobject]@{ Status = $status; Json = $json; Text = $text }
}

function Assert-That {
    param([string] $Name, [bool] $Condition, [string] $Detail = '')

    if ($Condition) {
        $script:passed++
        Write-Host "  [PASS] $Name" -ForegroundColor Green
    }
    else {
        $script:failed++
        Write-Host "  [FAIL] $Name $Detail" -ForegroundColor Red
    }
}

function Assert-Status {
    param([string] $Name, $Response, [int[]] $Expected)

    Assert-That $Name ($Expected -contains $Response.Status) `
        "(expected $($Expected -join '/'), got $($Response.Status): $($Response.Text))"
}

function Get-Items($Response) {
    if ($Response.Text.Trim() -eq '[]') { return @() }
    @($Response.Json)
}

function Get-Token([string] $Client, [string] $Secret) {
    $r = Invoke-Curl -Method POST -Url $TokenUrl -Form @{
        grant_type = 'client_credentials'; client_id = $Client; client_secret = $Secret }
    if ($r.Status -ne 200) { throw "Token request for '$Client' failed: $($r.Status) $($r.Text)" }
    $r.Json.access_token
}

function Section([string] $Title) { Write-Host "`n== $Title" -ForegroundColor Cyan }

try {
    $run = [guid]::NewGuid().ToString('N').Substring(0, 8)

    Section 'Authentication and authorization'
    $anon = Invoke-Curl -Method GET -Url "$Api/api/customer"
    Assert-Status 'No token is rejected with 401' $anon 401

    $reader = Get-Token $ReaderClient $ReaderSecret
    $denied = Invoke-Curl -Method GET -Url "$Api/api/customer" -Token $reader
    Assert-Status 'Role without grants gets 403' $denied 403

    $t = Get-Token $EditorClient $EditorSecret
    $bad = Invoke-Curl -Method POST -Url $TokenUrl -Form @{
        grant_type = 'client_credentials'; client_id = $EditorClient; client_secret = 'wrong' }
    Assert-Status 'Wrong client secret gets 401' $bad 401

    Section 'Customers'
    $email = "ada-$run@example.com"
    $c = Invoke-Curl -Method POST -Url "$Api/api/customer" -Token $t -Body @{ name = 'Ada'; email = $email }
    Assert-Status 'Create customer' $c 201
    $customerId = $c.Json.id
    Assert-That 'Customer has an id' ([bool]$customerId)

    $dup = Invoke-Curl -Method POST -Url "$Api/api/customer" -Token $t -Body @{ name = 'Ada 2'; email = $email }
    Assert-Status 'Duplicate email is rejected' $dup 409, 422

    $noName = Invoke-Curl -Method POST -Url "$Api/api/customer" -Token $t -Body @{ email = "x-$run@example.com" }
    Assert-Status 'Missing required name is rejected' $noName 422

    $emptyName = Invoke-Curl -Method POST -Url "$Api/api/customer" -Token $t -Body @{ name = ''; email = "y-$run@example.com" }
    Assert-Status 'Empty name violates minLength' $emptyName 422

    $g = Invoke-Curl -Method GET -Url "$Api/api/customer/$customerId" -Token $t
    Assert-Status 'Get customer' $g 200
    Assert-That 'Customer email round-trips' ($g.Json.email -eq $email)

    $u = Invoke-Curl -Method PATCH -Url "$Api/api/customer/$customerId" -Token $t -Body @{ name = 'Ada Lovelace' }
    Assert-Status 'Patch customer' $u 200
    Assert-That 'Customer name updated' ($u.Json.name -eq 'Ada Lovelace')

    $missing = Invoke-Curl -Method GET -Url "$Api/api/customer/$([guid]::NewGuid())" -Token $t
    Assert-Status 'Unknown customer returns 404' $missing 404

    $unknown = Invoke-Curl -Method GET -Url "$Api/api/doesnotexist" -Token $t
    Assert-Status 'Unknown entity is rejected' $unknown 400, 403, 404

    Section 'Tickets: defaults and computed fields'
    $tk = Invoke-Curl -Method POST -Url "$Api/api/ticket" -Token $t -Body @{ customerId = $customerId; subject = 'Cannot log in' }
    Assert-Status 'Create ticket' $tk 201
    $ticketId = $tk.Json.id
    Assert-That 'Initial status is open' ($tk.Json.status -eq 'open')
    Assert-That 'Default priority is 2' ($tk.Json.priority -eq 2)
    Assert-That 'Default cost is 40 (1h x 40)' ([decimal]$tk.Json.cost -eq 40)

    $big = Invoke-Curl -Method POST -Url "$Api/api/ticket" -Token $t -Body @{ customerId = $customerId; subject = 'Big job'; hours = 3; hourlyRate = 50 }
    Assert-Status 'Create ticket with explicit hours and rate' $big 201
    Assert-That 'Cost is 150 (3h x 50)' ([decimal]$big.Json.cost -eq 150)

    $setCost = Invoke-Curl -Method POST -Url "$Api/api/ticket" -Token $t -Body @{ customerId = $customerId; subject = 'Cheat'; cost = 1 }
    Assert-Status 'Writing the computed cost is rejected' $setCost 400, 422

    $recalc = Invoke-Curl -Method PATCH -Url "$Api/api/ticket/$ticketId" -Token $t -Body @{ hours = 2 }
    Assert-Status 'Patch hours' $recalc 200
    Assert-That 'Cost recomputed to 80' ([decimal]$recalc.Json.cost -eq 80)

    Section 'Tickets: validation and business rule'
    $prio = Invoke-Curl -Method POST -Url "$Api/api/ticket" -Token $t -Body @{ customerId = $customerId; subject = 'P'; priority = 5 }
    Assert-Status 'Priority above max is rejected' $prio 422

    $neg = Invoke-Curl -Method POST -Url "$Api/api/ticket" -Token $t -Body @{ customerId = $customerId; subject = 'N'; hours = -1 }
    Assert-Status 'Negative hours is rejected' $neg 422

    $noSubject = Invoke-Curl -Method POST -Url "$Api/api/ticket" -Token $t -Body @{ customerId = $customerId }
    Assert-Status 'Missing subject is rejected' $noSubject 422

    $over = Invoke-Curl -Method POST -Url "$Api/api/ticket" -Token $t -Body @{ customerId = $customerId; subject = 'Rebuild'; hours = 41 }
    Assert-Status 'More than 40 hours is rejected by the rule' $over 422
    Assert-That 'Rule error code is MTPR-2011' ($over.Text -match 'MTPR-2011')

    $limit = Invoke-Curl -Method POST -Url "$Api/api/ticket" -Token $t -Body @{ customerId = $customerId; subject = 'Limit'; hours = 40 }
    Assert-Status 'Exactly 40 hours is allowed' $limit 201

    $orphan = Invoke-Curl -Method POST -Url "$Api/api/ticket" -Token $t -Body @{ customerId = [guid]::NewGuid().ToString(); subject = 'Orphan' }
    Assert-Status 'Unknown customer reference is rejected' $orphan 400, 409, 422

    Section 'Tickets: lifecycle'
    function Set-Status([string] $Id, [string] $Status) {
        Invoke-Curl -Method PATCH -Url "$Api/api/ticket/$Id" -Token $t -Body @{ status = $Status }
    }

    Assert-Status 'open -> closed is not allowed' (Set-Status $ticketId 'closed') 422
    Assert-Status 'open -> resolved is not allowed' (Set-Status $ticketId 'resolved') 422
    Assert-Status 'Unknown state is rejected' (Set-Status $ticketId 'nonsense') 422
    Assert-Status 'open -> inProgress' (Set-Status $ticketId 'inProgress') 200
    Assert-Status 'inProgress -> resolved' (Set-Status $ticketId 'resolved') 200
    Assert-Status 'resolved -> inProgress (reopen)' (Set-Status $ticketId 'inProgress') 200
    Assert-Status 'inProgress -> resolved again' (Set-Status $ticketId 'resolved') 200
    Assert-Status 'resolved -> closed' (Set-Status $ticketId 'closed') 200
    Assert-Status 'closed -> open is not allowed' (Set-Status $ticketId 'open') 422
    $final = Invoke-Curl -Method GET -Url "$Api/api/ticket/$ticketId" -Token $t
    Assert-That 'Final status is closed' ($final.Json.status -eq 'closed')

    Section 'Comments'
    $liveTicket = $big.Json.id
    $cm1 = Invoke-Curl -Method POST -Url "$Api/api/ticketcomment" -Token $t -Body @{ ticketId = $liveTicket; body = 'Looking into it' }
    Assert-Status 'Create comment' $cm1 201
    $cm2 = Invoke-Curl -Method POST -Url "$Api/api/ticketcomment" -Token $t -Body @{ ticketId = $liveTicket; body = 'Fixed' }
    Assert-Status 'Create second comment' $cm2 201

    $emptyBody = Invoke-Curl -Method POST -Url "$Api/api/ticketcomment" -Token $t -Body @{ ticketId = $liveTicket; body = '' }
    Assert-Status 'Empty comment is rejected' $emptyBody 422

    $noTicket = Invoke-Curl -Method POST -Url "$Api/api/ticketcomment" -Token $t -Body @{ ticketId = [guid]::NewGuid().ToString(); body = 'x' }
    Assert-Status 'Comment on unknown ticket is rejected' $noTicket 400, 409, 422

    Section 'Querying'
    $list = Invoke-Curl -Method GET -Url "$Api/api/ticketcomment?filter=ticketId:eq:$liveTicket" -Token $t
    Assert-Status 'List comments by ticket' $list 200
    Assert-That 'Two comments found' ((Get-Items $list).Count -eq 2) $list.Text

    $paged = Invoke-Curl -Method GET -Url "$Api/api/ticket?filter=customerId:eq:$customerId&sort=-cost&take=2&fields=id,cost" -Token $t
    Assert-Status 'Filter, sort, page and project tickets' $paged 200

    $closed = Invoke-Curl -Method GET -Url "$Api/api/ticket?filter=customerId:eq:$customerId&filter=status:eq:closed" -Token $t
    Assert-Status 'Combine two filters' $closed 200
    Assert-That 'Closed filter finds the ticket' ($closed.Text -match $ticketId)

    $badFilter = Invoke-Curl -Method GET -Url "$Api/api/ticket?filter=nope:eq:1" -Token $t
    Assert-Status 'Unknown filter field returns 400' $badFilter 400
    Assert-That 'Filter error code is MTPR-2010' ($badFilter.Text -match 'MTPR-2010')

    Section 'Deletion'
    $blocked = Invoke-Curl -Method DELETE -Url "$Api/api/customer/$customerId" -Token $t
    Assert-Status 'Customer with tickets cannot be deleted' $blocked 409, 422

    $delTicket = Invoke-Curl -Method DELETE -Url "$Api/api/ticket/$liveTicket" -Token $t
    Assert-Status 'Delete ticket' $delTicket 204
    $after = Invoke-Curl -Method GET -Url "$Api/api/ticketcomment?filter=ticketId:eq:$liveTicket" -Token $t
    Assert-That 'Comments cascade-deleted with the ticket' ((Get-Items $after).Count -eq 0) $after.Text

    $others = Invoke-Curl -Method GET -Url "$Api/api/ticket?filter=customerId:eq:$customerId&take=100" -Token $t
    foreach ($row in (Get-Items $others)) {
        [void](Invoke-Curl -Method DELETE -Url "$Api/api/ticket/$($row.id)" -Token $t)
    }

    $delCustomer = Invoke-Curl -Method DELETE -Url "$Api/api/customer/$customerId" -Token $t
    Assert-Status 'Delete customer once tickets are gone' $delCustomer 204
    $gone = Invoke-Curl -Method GET -Url "$Api/api/customer/$customerId" -Token $t
    Assert-Status 'Deleted customer returns 404' $gone 404
}
catch {
    $script:failed++
    Write-Host "  [ERROR] $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    Remove-Item -Recurse -Force $script:tmp -ErrorAction SilentlyContinue
}

Write-Host "`nPassed: $script:passed  Failed: $script:failed" -ForegroundColor $(if ($script:failed) { 'Red' } else { 'Green' })
exit $(if ($script:failed) { 1 } else { 0 })
```
