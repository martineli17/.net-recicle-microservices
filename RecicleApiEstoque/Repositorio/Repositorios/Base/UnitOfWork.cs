using Crosscuting.Notificacao;
using Dominio.Contratos.Repositorios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Repositorio.Contexto;
using Repositorio.Sincronizacao;
using System;
using System.Threading.Tasks;

namespace Repositorio.Repositorios.Base
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ContextoEntity _contextoEntity;
        private readonly INotificador _notificador;
        private readonly ISincronizacaoEvent _sincronizador;

        public UnitOfWork(ContextoEntity contextoEntity,
            INotificador notificador,
            ISincronizacaoEvent sincronizador)
        {
            _contextoEntity = contextoEntity;
            _notificador = notificador;
            _sincronizador = sincronizador;
        }

        public async Task<bool> CommitAsync()
        {
            using IDbContextTransaction transaction = _contextoEntity.Database.BeginTransaction();
            try
            {
                var changes = await _contextoEntity.SaveChangesAsync();
                if (changes > 0)
                    await _sincronizador.SincronizarAsync();
                await transaction.CommitAsync();
                return changes > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await transaction.RollbackAsync();
                _notificador.Add("Ocorreu um erro ao processar a operação.", EnumTipoMensagem.Erro);
                return false;
            }
            finally
            {
                //usar em banco de dados relacional e que nao seja em memoria
                _contextoEntity.Database.GetDbConnection().Close();
            }
        }
    }
}
