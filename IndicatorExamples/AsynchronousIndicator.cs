using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer;

namespace AsynchronousIndicator;

public class AsynchronousIndicator : Indicator
{
    [InputParameter]
    public int Delay { get; set; } = 200;

    private readonly Queue<Action> buffer;
    private readonly object bufferLocker;

    private readonly ManualResetEvent resetEvent;
    private CancellationTokenSource cts;

    public AsynchronousIndicator()
    {
        this.Name = nameof(AsynchronousIndicator);
        this.SeparateWindow = true;
        this.UpdateType = IndicatorUpdateType.OnBarClose;

        this.AddLineSeries();

        this.buffer = new Queue<Action>();
        this.bufferLocker = new object();
        this.resetEvent = new ManualResetEvent(false);
    }

    protected override void OnInit()
    {
        this.cts = new CancellationTokenSource();
        Task.Factory.StartNew(this.Process, this.cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    protected override void OnUpdate(UpdateArgs args)
    {
        int currentIndex = this.Count;
        double value = this.Median();

        lock (this.bufferLocker)
        {
            this.buffer.Enqueue(() =>
            {
                // some heavy operation
                Task.Delay(this.Delay).Wait();

                this.SetValue(value, offset: this.Count - currentIndex);
            });
        }

        this.resetEvent.Set();
    }

    protected override void OnClear()
    {
        this.cts?.Cancel();

        lock (this.bufferLocker)
            this.buffer.Clear();

        this.resetEvent.Set();
    }

    private void Process()
    {
        while (true)
        {
            try
            {
                this.resetEvent.WaitOne();

                if (this.cts?.IsCancellationRequested ?? false)
                    return;

                while (this.buffer.Count > 0)
                {
                    if (this.cts?.IsCancellationRequested ?? false)
                        return;

                    try
                    {
                        Action action;

                        lock (this.bufferLocker)
                            action = this.buffer.Dequeue();

                        action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Core.Loggers.Log(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Loggers.Log(ex);
            }
            finally
            {
                this.resetEvent.Reset();
            }
        }
    }
}
