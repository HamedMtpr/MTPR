namespace Mtpr.Core.Repositories;

public interface IRepository
{
}

public interface IRepository<TEntity> : ICommandRepository<TEntity, TEntity, TEntity, TEntity>,
    IQueryRepository<TEntity>
{

}

public interface IRepository<in TCreateDto, in TUpdateDto, in TDeleteDto, in TGetDto, in TGeAllDto, out TEntity> :
    ICommandRepository<TCreateDto, TUpdateDto, TDeleteDto, TEntity>,
    IQueryRepository<TGetDto, TGeAllDto, TEntity>
{
}

public interface ICommandRepository : IRepository
{
}

public interface ICommandRepository<in TCreateDto, in TUpdateDto, in TDeleteDto, out TEntity> : ICreateCommandRepository<TCreateDto,TEntity>, 
    IUpdateCommandRepository<TUpdateDto, TEntity>, 
    IDeleteCommandRepository<TDeleteDto, TEntity>
{
}

public interface ICreateCommandRepository<in TCreateDto, out TEntity> : ICommandRepository
{
    TEntity Create(TCreateDto createDto);
}

public interface IUpdateCommandRepository<in TUpdateDto, out TEntity> : ICommandRepository
{
    TEntity Update(TUpdateDto createDto);
}

public interface IDeleteCommandRepository<in TDeleteDto, out TEntity> : ICommandRepository
{
    TEntity Delete(TDeleteDto deleteDto);
}

public interface IQueryRepository : IRepository
{
}

public interface IQueryRepository<in TGetGto, in TGetAllDto, out TEntity> : IGetQueryRepository<TGetGto, TEntity>,
    IGetAllQueryRepository<TGetAllDto, TEntity>
{
}

public interface IQueryRepository<out TEntity> : IGetQueryRepository<TEntity>, IGetAllQueryRepository<TEntity>
{
}

public interface IGetQueryRepository<in TGetDto ,out TEntity> : IQueryRepository
{
    TEntity Get(TGetDto getQuery);
}

public interface IGetAllQueryRepository<in TGetAllDto,out TEntity> : IQueryRepository
{
    IEnumerable<TEntity> GetAll(TGetAllDto getAllQuery);
}

public interface IGetQueryRepository<out TEntity> : IQueryRepository
{
    TEntity Get(GetQuery getQuery);
}

public interface IGetAllQueryRepository<out TEntity> : IQueryRepository
{
    IEnumerable<TEntity> GetAll(GetAllQuery getAllQuery);
}