using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    public class MonsterGrain : WandererGrain, IMonsterGrain
    {
        const string MONSTER_IMAGE_PATH = "/img/monster.png";
        const string WINK_IMAGE_PATH = "/img/wink.png";

        public MonsterGrain(
            [PersistentState("wanderer", "wandererStorage")] IPersistentState<Wanderer> wanderer, 
            ILogger<WandererGrain> logger) : base(wanderer, logger)
        {
        }

        public async Task Eat(IWanderGrain grain)
        {
            var deadWanderer = await grain.GetWanderer();
            deadWanderer.State = WandererState.Dead;
            await grain.SetInfo(deadWanderer);
            grain.Dispose();
            await SpeedUp(50);
        }

        public override async Task SetInfo(Wanderer wanderer)
        {
            Wanderer.State.AvatarImageUrl = MONSTER_IMAGE_PATH;
            await base.SetInfo(wanderer);
        }

        public override async Task SetLocation(ITileGrain tileGrain)
        {
            // before the monster leaves, he must eat
            var tile = await tileGrain.GetTile();

            // eat the first wanderer you see, if there are any
            var theUnfortunate = tile.WanderersHere.FirstOrDefault();
            if(theUnfortunate != null && theUnfortunate.Name != ((Grain)this).GetPrimaryKeyString())
            {
                Wanderer.State.AvatarImageUrl = WINK_IMAGE_PATH;
                await base.SetLocation(tileGrain);

                var unfortunateGrain = GrainFactory.GetGrain<IWanderGrain>(theUnfortunate.Name, typeof(WandererGrain).FullName);
                await Eat(unfortunateGrain);
                await tileGrain.Leaves(unfortunateGrain);
            }

            Wanderer.State.AvatarImageUrl = MONSTER_IMAGE_PATH;
            await base.SetLocation(tileGrain);
        }
    }
}
