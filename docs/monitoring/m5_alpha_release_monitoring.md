# m5_alpha_release: monitoring

- [Back to index](index.md)
- [M5 operations](../operations/m5_alpha_release_operations.md)
- [M5 deployment](../deployment/m5_alpha_release_deployment.md)
- [M5 build and run](../building/m5_alpha_release_build.md)

The alpha has no metrics endpoint or health check. Monitor it with the signals below.

| Signal | How to observe it |
| --- | --- |
| Process alive | Process supervisor or container restart policy; `docker ps` for the container. |
| Logs | Standard output of the API and token service; `docker logs <container>`. |
| Authentication failures | Observe HTTP 401 responses in the client or configured reverse-proxy access logs. Check that the token service is reachable and its issuer matches the API authority. |
| Authorization failures | Observe HTTP 403 responses in the client or configured reverse-proxy access logs. Inspect recorded denials in `__mtpr_audit` and check the relevant role, field and tenant policies. |
| Definition errors | Error responses or log entries after editing a file in the definitions folder; revert the edit if requests fail. |
| Database | Size and free space of the `/data` volume or the SQLite file. |
| Token service | `GET /.well-known/openid-configuration` returns 200 over HTTPS. |

The default API configuration does not provide request access logs and does not guarantee that 401 or 403 responses appear in application logs. Authentication failures are not written to the audit table. The audit table records the authorization denials described in the operations guide; it is not a complete security-event log.
