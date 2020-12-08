﻿// FIXME: This class is brought from Libplanet.Tests.
// Should be published on NuGet instead of being included in this project. See:
// https://github.com/planetarium/libplanet/blob/85252a03486435d18a3a1356166d606b8024fd6b/Libplanet.Tests/Common/RenderRecord.cs
using System;
using Libplanet.Action;

namespace Lib9c.Renderer
{
    public abstract class RenderRecord<T>
        where T : IAction, new()
    {
        public long Index;

        public string StackTrace;

        public override string ToString() => $"{Index}.";

        public abstract class ActionBase : RenderRecord<T>
        {
            public IAction Action;

            public IActionContext Context;

            public bool Render;

            public bool Unrender
            {
                get => !Render;
                set => Render = !value;
            }

            public override string ToString() =>
                $"{base.ToString()} #{Context.BlockIndex} " +
                (Render ? "Render" : "Unrender") + "Action";
        }

        public class ActionSuccess : ActionBase
        {
            public IAccountStateDelta NextStates;

            public override string ToString() => $"{base.ToString()} [success]";
        }

        public class ActionError : ActionBase
        {
            public Exception Exception;

            public override string ToString() => $"{base.ToString()} [error]";
        }

        public abstract class BlockBase : RenderRecord<T>
        {
            public Libplanet.Blocks.Block<T> OldTip;
            public Libplanet.Blocks.Block<T> NewTip;
            public bool Begin;

            public bool End
            {
                get => !Begin;
                set => Begin = !value;
            }

            public override string ToString() =>
                $"{base.ToString()} " +
                $"#{OldTip.Index} {OldTip.Hash} -> #{NewTip.Index} {NewTip.Hash} Render..." +
                (End ? "End" : string.Empty);
        }

        public class Block : BlockBase
        {
            public override string ToString() =>
                base.ToString().Replace("Render...", "RenderBlock");
        }

        public class Reorg : BlockBase
        {
            public Libplanet.Blocks.Block<T> Branchpoint;

            public override string ToString() =>
                base.ToString().Replace("Render...", "RenderReorg") +
                $" [branchpoint: #{Branchpoint.Index} {Branchpoint.Hash}]";
        }
    }
}
