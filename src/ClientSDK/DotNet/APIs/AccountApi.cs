using System;
using System.Reflection;
using System.Threading.Tasks;
using Raid.Toolkit.DataModel;

namespace Raid.Client
{
    public class AccountApi : ApiBase<IAccountApi>, IAccountApi
    {
        public AccountApi(RaidToolkitClientBase client) : base(client) { }

#pragma warning disable 0067
        public event EventHandler<SerializableEventArgs> Updated;
#pragma warning restore 0067

        public Task<Account> GetAccount(string accountId)
        {
            return CallMethod<Account>(MethodBase.GetCurrentMethod(), accountId);
        }

        public Task<RaidExtractor.Core.AccountDump> GetAccountDump(string accountId)
        {
            return CallMethod<RaidExtractor.Core.AccountDump>(MethodBase.GetCurrentMethod(), accountId);
        }

        public Task<Account[]> GetAccounts()
        {
            return CallMethod<Account[]>(MethodBase.GetCurrentMethod());
        }

        public Task<Resources> GetAllResources(string accountId)
        {
            return CallMethod<Resources>(MethodBase.GetCurrentMethod(), accountId);
        }

        public Task<Artifact> GetArtifactById(string accountId, int artifactId)
        {
            return CallMethod<Artifact>(MethodBase.GetCurrentMethod(), accountId, artifactId);
        }

        public Task<Artifact[]> GetArtifacts(string accountId)
        {
            return CallMethod<Artifact[]>(MethodBase.GetCurrentMethod(), accountId);
        }

        public Task<Hero> GetHeroById(string accountId, int heroId, bool snapshot = false)
        {
            return CallMethod<Hero>(MethodBase.GetCurrentMethod(), accountId, heroId, snapshot);
        }

        public Task<Hero[]> GetHeroes(string accountId, bool snapshot = false)
        {
            return CallMethod<Hero[]>(MethodBase.GetCurrentMethod(), accountId, snapshot);
        }

        public Task<ArenaData> GetArena(string accountId)
        {
            return CallMethod<ArenaData>(MethodBase.GetCurrentMethod(), accountId);
        }

        public Task<AcademyData> GetAcademy(string accountId)
        {
            return CallMethod<AcademyData>(MethodBase.GetCurrentMethod(), accountId);
        }
    }
}
