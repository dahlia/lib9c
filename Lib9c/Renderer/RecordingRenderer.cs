﻿// FIXME: This class is brought from Libplanet.Tests.
// Should be published on NuGet instead of being included in this project. See:
// https://github.com/planetarium/libplanet/blob/85252a03486435d18a3a1356166d606b8024fd6b/Libplanet.Tests/Common/RecordingRenderer.cs
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet.Action;
using Libplanet.Blockchain.Renderers;
using Libplanet.Blocks;
using Serilog;

namespace Lib9c.Renderer
{
    public class RecordingRenderer<T> : IActionRenderer<T>
        where T : IAction, new()
    {
        private List<RenderRecord<T>> _records;
        private long _nextIndex;

        public RecordingRenderer()
        {
            _nextIndex = 0;
            _records = new List<RenderRecord<T>>();
        }

        public EventHandler<IAction> RenderEventHandler { get; set; }

        public IReadOnlyList<RenderRecord<T>> Records => _records;

        public IReadOnlyList<RenderRecord<T>.ActionBase> ActionRecords =>
            Records.OfType<RenderRecord<T>.ActionBase>().ToImmutableArray();

        public IReadOnlyList<RenderRecord<T>.ActionSuccess> ActionSuccessRecords =>
            Records.OfType<RenderRecord<T>.ActionSuccess>().ToImmutableArray();

        public IReadOnlyList<RenderRecord<T>.ActionError> ActionErrorRecords =>
            Records.OfType<RenderRecord<T>.ActionError>().ToImmutableArray();

        public IReadOnlyList<RenderRecord<T>.Block> BlockRecords =>
            Records.OfType<RenderRecord<T>.Block>().ToImmutableArray();

        public IReadOnlyList<RenderRecord<T>.Reorg> ReorgRecords =>
            Records.OfType<RenderRecord<T>.Reorg>().ToImmutableArray();

        public void ResetRecords()
        {
            _nextIndex = 0;
            _records.Clear();
            Log.Logger.ForContext<RecordingRenderer<T>>().Debug("Reset records.");
        }

        public virtual void RenderAction(
            IAction action,
            IActionContext context,
            IAccountStateDelta nextStates
        )
        {
            _records.Add(new RenderRecord<T>.ActionSuccess
            {
                Index = _nextIndex++,
                StackTrace = RemoveFirstLine(Environment.StackTrace).TrimEnd(),
                Action = action,
                Context = context,
                NextStates = nextStates,
                Render = true,
            });

            RenderEventHandler?.Invoke(action, action);
        }

        public virtual void RenderActionError(
            IAction action,
            IActionContext context,
            Exception exception
        ) =>
            _records.Add(new RenderRecord<T>.ActionError
            {
                Index = _nextIndex++,
                StackTrace = RemoveFirstLine(Environment.StackTrace).TrimEnd(),
                Action = action,
                Context = context,
                Exception = exception,
                Render = true,
            });

        public virtual void UnrenderAction(
            IAction action,
            IActionContext context,
            IAccountStateDelta nextStates
        ) =>
            _records.Add(new RenderRecord<T>.ActionSuccess
            {
                Index = _nextIndex++,
                StackTrace = RemoveFirstLine(Environment.StackTrace).TrimEnd(),
                Unrender = true,
                Action = action,
                Context = context,
                NextStates = nextStates,
            });

        public virtual void UnrenderActionError(
            IAction action,
            IActionContext context,
            Exception exception
        ) =>
            _records.Add(new RenderRecord<T>.ActionError
            {
                Index = _nextIndex++,
                StackTrace = RemoveFirstLine(Environment.StackTrace).TrimEnd(),
                Unrender = true,
                Action = action,
                Context = context,
                Exception = exception,
            });

        public virtual void RenderBlock(Block<T> oldTip, Block<T> newTip) =>
            _records.Add(new RenderRecord<T>.Block
            {
                Index = _nextIndex++,
                StackTrace = RemoveFirstLine(Environment.StackTrace).TrimEnd(),
                Begin = true,
                OldTip = oldTip,
                NewTip = newTip,
            });

        public virtual void RenderBlockEnd(Block<T> oldTip, Block<T> newTip) =>
            _records.Add(new RenderRecord<T>.Block
            {
                Index = _nextIndex++,
                StackTrace = RemoveFirstLine(Environment.StackTrace).TrimEnd(),
                End = true,
                OldTip = oldTip,
                NewTip = newTip,
            });

        public virtual void RenderReorg(Block<T> oldTip, Block<T> newTip, Block<T> branchpoint) =>
            _records.Add(new RenderRecord<T>.Reorg
            {
                Index = _nextIndex++,
                StackTrace = RemoveFirstLine(Environment.StackTrace).TrimEnd(),
                Begin = true,
                OldTip = oldTip,
                NewTip = newTip,
                Branchpoint = branchpoint,
            });

        public virtual void RenderReorgEnd(
            Block<T> oldTip,
            Block<T> newTip,
            Block<T> branchpoint
        ) =>
            _records.Add(new RenderRecord<T>.Reorg
            {
                Index = _nextIndex++,
                StackTrace = RemoveFirstLine(Environment.StackTrace).TrimEnd(),
                End = true,
                OldTip = oldTip,
                NewTip = newTip,
                Branchpoint = branchpoint,
            });

        private static string RemoveFirstLine(string stackTrace)
        {
            int pos = stackTrace.IndexOf('\n');
            return pos < 0 ? stackTrace : stackTrace.Substring(pos + 1);
        }
    }
}
