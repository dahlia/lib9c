using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("sell")]
    public class Sell : GameAction
    {
        public Address sellerAvatarAddress;
        public Guid itemId;
        public FungibleAssetValue price;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["sellerAvatarAddress"] = sellerAvatarAddress.Serialize(),
            ["itemId"] = itemId.Serialize(),
            ["price"] = price.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            sellerAvatarAddress = plainValue["sellerAvatarAddress"].ToAddress();
            itemId = plainValue["itemId"].ToGuid();
            price = plainValue["price"].ToFungibleAssetValue();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(ShopState.Address, MarkChanged);
                states = states.SetState(sellerAvatarAddress, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }
            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("Sell exec started.");


            if (price.Sign < 0)
            {
                throw new InvalidPriceException($"Aborted as the price is less than zero: {price}.");
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, sellerAvatarAddress, out AgentState agentState, out AvatarState avatarState))
            {
                throw new FailedLoadStateException("Aborted as the avatar state of the signer was failed to load.");
            }
            sw.Stop();
            Log.Debug("Sell Get AgentAvatarStates: {Elapsed}", sw.Elapsed);
            sw.Restart();

            if (!avatarState.worldInformation.IsStageCleared(GameConfig.RequireClearedStageLevel.ActionsInShop))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(GameConfig.RequireClearedStageLevel.ActionsInShop, current);
            }

            if (!states.TryGetState(ShopState.Address, out Bencodex.Types.Dictionary d))
            {
                throw new FailedLoadStateException("Aborted as the shop state was failed to load.");
            }
            var shopState = new ShopState(d);
            sw.Stop();
            Log.Debug("Sell Get ShopState: {Elapsed}", sw.Elapsed);
            sw.Restart();

            Log.Debug("Execute Sell; seller: {SellerAvatarAddress}", sellerAvatarAddress);

            // 인벤토리에서 판매할 아이템을 선택하고 수량을 조절한다.
            if (!avatarState.inventory.TryGetNonFungibleItem(itemId, out ItemUsable nonFungibleItem))
            {
                throw new ItemDoesNotExistException(
                    $"Aborted as the NonFungibleItem ({itemId}) was failed to load from avatar's inventory."
                );
            }

            if (nonFungibleItem.RequiredBlockIndex > context.BlockIndex)
            {
                throw new RequiredBlockIndexException(
                    $"Aborted as the equipment to enhance ({itemId}) is not available yet; it will be available at the block #{nonFungibleItem.RequiredBlockIndex}."
                );
            }

            avatarState.inventory.RemoveNonFungibleItem(nonFungibleItem);
            if (nonFungibleItem is Equipment equipment)
            {
                equipment.equipped = false;
            }

            var productId = context.Random.GenerateRandomGuid();

            shopState.Register(new ShopItem(
                ctx.Signer,
                sellerAvatarAddress,
                productId,
                nonFungibleItem,
                price
            ));

            sw.Stop();
            Log.Debug("Sell Get Register Item: {Elapsed}", sw.Elapsed);
            sw.Restart();

            avatarState.updatedAt = ctx.BlockIndex;
            avatarState.blockIndex = ctx.BlockIndex;

            states = states.SetState(sellerAvatarAddress, avatarState.Serialize());
            sw.Stop();
            Log.Debug("Sell Set AvatarState: {Elapsed}", sw.Elapsed);
            sw.Restart();

            states = states.SetState(ShopState.Address, shopState.Serialize());
            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("Sell Set ShopState: {Elapsed}", sw.Elapsed);
            Log.Debug("Sell Total Executed Time: {Elapsed}", ended - started);

            return states;
        }
    }
}
