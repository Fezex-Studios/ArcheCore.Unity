using System.Threading.Tasks;
using ArcheCore.WorldServer.PersistenceServer.Packets;
using Worldserver.ArcheCore.PersistenceServer.Scripts;

namespace ArcheCore.WorldServer.PersistenceServer.Senders
{
    public class W2PCharacterSender
    {
        private readonly PersistenceClient _client;

        public W2PCharacterSender(PersistenceClient client)
            => _client = client;

        public async Task<CharacterLoadResponse> Load(long characterId)
        {
            var tcs = new TaskCompletionSource<CharacterLoadResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            _client.pendingLoads[characterId] = tcs;

            await _client.Send(PersistenceOpcode.CharacterLoad, new CharacterLoadRequest
            {
                CharacterId = characterId
            });

            return await tcs.Task;
        }

        public async Task Save(long characterId, string name, int level, float x, float y, float z)
        {
            await _client.Send(PersistenceOpcode.CharacterSave, new CharacterSaveRequest
            {
                CharacterId = characterId,
                Name        = name,
                Level       = level,
                X           = x,
                Y           = y,
                Z           = z
            });
        }
    }
}