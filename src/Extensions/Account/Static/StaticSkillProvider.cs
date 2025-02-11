using System;
using System.Linq;
using Raid.Toolkit.DataModel;
using Raid.Toolkit.Extensibility;
using Raid.Toolkit.Extensibility.DataServices;
using Raid.Toolkit.Extensibility.Providers;
using Il2CppToolkit.Runtime;

namespace Raid.Toolkit.Extension.Account
{
    public class StaticSkillProvider : DataProvider<StaticDataContext, StaticSkillData>
    {
        private static Version kVersion = new(2, 0);

        public override string Key => "skills";
        public override Version Version => kVersion;

        private readonly CachedDataStorage<PersistedDataStorage> Storage;
        public StaticSkillProvider(CachedDataStorage<PersistedDataStorage> storage)
        {
            Storage = storage;
        }

        public override bool Update(Il2CsRuntimeContext runtime, StaticDataContext context)
        {
            ModelScope scope = new(runtime);
            var hash = scope.StaticDataManager._hash;
            if (Storage.TryRead(context, Key, out StaticSkillData? staticSkillData))
            {
                if (staticSkillData?.Hash == hash)
                    return false;
            }
            try
            {
                var staticData = scope.StaticDataManager.StaticData;
                staticSkillData = new()
                {
                    Hash = hash,
                    SkillTypes = staticData.SkillData._skillTypeById.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToModel())
                };
            }
            catch { }
            return Storage.Write(context, Key, staticSkillData);
        }
    }
}
