using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MunNovel.Command;
using MunNovel.Executor;
using MunNovel.SharpScript;
using CyanStars.Framework.Timer;

namespace CyanStars.Framework.Dialogue
{
    public delegate void ScriptExecutorStateChanged(ScriptExecuteState oldState, ScriptExecuteState newState);

    public partial class UnitySharpScriptExecutor : IScriptExecutor
    {
        public event ScriptExecutorStateChanged OnStateChanged;

        private ScriptExecuteState state;
        public ScriptExecuteState State
        {
            get => state;
            set
            {
                if (state != value)
                {
                    ScriptExecuteState oldState = state;
                    state = value;
                    OnStateChanged?.Invoke(oldState, value);
                }
            }
        }

        private IScript script;
        private IScriptContext context;
        private IScriptExecutorHandler actionHandler;

        private PauseContext pauseContext;

        private readonly List<Task> ExecutingTasks = new List<Task>(10);
        public readonly List<ICommand> ExecutedCommands = new List<ICommand>(10);

        private CancellationTokenSource cancelCommandCts;
        private CancellationToken CommandCancelToken
        {
            get
            {
                if (this.cancelCommandCts?.IsCancellationRequested ?? true)
                {
                    this.cancelCommandCts = new CancellationTokenSource();
                }
                return this.cancelCommandCts.Token;
            }
        }


        public UnitySharpScriptExecutor(IScriptExecutorHandler actionHandler)
        {
            context = new ScriptContext(new CommandBuffer(this));
            this.actionHandler = actionHandler;
        }

        public UnitySharpScriptExecutor(IScriptContext context, IScriptExecutorHandler actionHandler)
        {
            this.context = context;
            this.actionHandler = actionHandler;
        }

        public async Task Load(IScript script, CancellationToken cancellationToken = default)
        {
            if (script is null)
            {
                throw new ArgumentNullException(nameof(script));
            }

            this.script = script;
            State = ScriptExecuteState.Loading;

            await script.PreLoad(cancellationToken);
            State = ScriptExecuteState.Loaded;
        }

        public void Play()
        {
            if (State == ScriptExecuteState.NoScript)
                throw new ScriptExecuteException("Not load Script");

            if (State == ScriptExecuteState.Playing || State == ScriptExecuteState.Done)
                return;

            if (State == ScriptExecuteState.Loading)
                throw new ScriptExecuteException("Script loading");

            State = ScriptExecuteState.Playing;
            Execute();
        }

        public bool TryCompleteCurrentExecuting()
        {
            if (this.ExecutingTasks.Count > 0 && !this.cancelCommandCts.IsCancellationRequested)
            {
                this.cancelCommandCts.Cancel();
                return true;
            }
            return false;
        }

        private async void Execute()
        {
            await script.Execute(context);
            State = ScriptExecuteState.Done;
        }

        private async void Pause(TaskCompletionSource<object> tcs, double time)
        {
            if (State != ScriptExecuteState.Playing)
            {
                UnityEngine.Debug.LogError("script is not playing state, can't pause");
                tcs.SetCanceled();
                return;
            }

            this.pauseContext = new PauseContext(tcs);
            State = ScriptExecuteState.Pause;

            // time <= 0 时，将暂停script执行的控制权移交给外部实现
            if (time <= 0)
            {
                await actionHandler.OnPause();
                Resume();
                return;
            }

            // 来自script设置的暂停
            void WaitPauseCallback(object userdata)
            {
                var task = (Task)userdata;
                if (!task.IsCompleted)
                {
                    Resume();
                }
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            this.pauseContext.CancellationSource = cts;
            GameRoot.Timer.GetTimer<IntervalTimer>().Add((float)time, WaitPauseCallback, tcs.Task, 1, cts.Token);
        }

        public void Resume()
        {
            if (this.pauseContext.IsNull)
                return;

            TaskCompletionSource<object> tcs = this.pauseContext.TaskSource;
            this.pauseContext = default;
            State = ScriptExecuteState.Playing;
            tcs.SetResult(null);
        }

        private void CommandsExecuted(IList<ICommand> commands)
        {
            if ((commands?.Count ?? 0) > 0)
            {
                this.ExecutedCommands.AddRange(commands);
                commands.Clear();
            }
        }
    }
}
