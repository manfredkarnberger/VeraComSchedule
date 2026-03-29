using VeraCom.Models;
using Peak.Can.Basic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PcanSqliteSender.Services
{
    public class PcanService
    {
        private ushort _handle = PCANBasic.PCAN_USBBUS1;

        private CancellationTokenSource _cts;
        private Task _sendTask;
        private Task _receiveTask;

        private Stopwatch _stopwatch;

        public bool IsRunning { get; private set; }

        public event Action<CanMessage> MessageSent;
        public event Action<CanMessage> MessageReceived;

        private class ScheduledMessage
        {
            public CanMessage Message;
            public long NextTick;
            public long IntervalTicks;
        }

        private List<ScheduledMessage> _schedule = new();

        public async Task StartAsync(IEnumerable<CanMessage> messages)
        {
            if (IsRunning) return;

            var res = PCANBasic.Initialize(_handle, TPCANBaudrate.PCAN_BAUD_500K);
            if (res != TPCANStatus.PCAN_ERROR_OK)
                throw new Exception("PCAN-Init fehlgeschlagen");

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _stopwatch = Stopwatch.StartNew();
            _schedule.Clear();

            foreach (var msg in messages)
            {
                var intervalTicks = (long)(msg.CycleTimeMs * Stopwatch.Frequency / 1000.0);

                _schedule.Add(new ScheduledMessage
                {
                    Message = msg,
                    IntervalTicks = intervalTicks,
                    NextTick = _stopwatch.ElapsedTicks + intervalTicks
                });
            }

            _sendTask = Task.Run(() => SchedulerLoop(token), token);
            _receiveTask = Task.Run(() => ReceiveLoop(token), token);

            IsRunning = true;
        }

        private void SchedulerLoop(CancellationToken token)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            while (!token.IsCancellationRequested)
            {
                long now = _stopwatch.ElapsedTicks;

                foreach (var item in _schedule)
                {
                    if (now >= item.NextTick)
                    {
                        Send(item.Message);

                        item.NextTick += item.IntervalTicks;

                        if (now > item.NextTick)
                            item.NextTick = now + item.IntervalTicks;
                    }
                }

                Thread.SpinWait(50);
            }
        }

        private void Send(CanMessage task)
        {
            TPCANMsg msg = new TPCANMsg
            {
                ID = task.CanID,
                LEN = task.DLC,
                DATA = new byte[8],
                MSGTYPE = TPCANMessageType.PCAN_MESSAGE_STANDARD
            };

            Array.Copy(task.Payload, msg.DATA, Math.Min(task.DLC, (byte)8));

            if (PCANBasic.Write(_handle, ref msg) == TPCANStatus.PCAN_ERROR_OK)
            {
                MessageSent?.Invoke(task);
            }
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                TPCANMsg msg;
                TPCANTimestamp ts;

                var res = PCANBasic.Read(_handle, out msg, out ts);

                if (res == TPCANStatus.PCAN_ERROR_OK)
                {
                    var canMsg = new CanMessage
                    {
                        CanID = msg.ID,
                        DLC = msg.LEN,
                        Payload = msg.DATA,
                        Timestamp = DateTime.Now
                    };

                    MessageReceived?.Invoke(canMsg);
                }

                await Task.Delay(1, token);
            }
        }

        public async Task StopAsync()
        {
            if (!IsRunning) return;

            _cts.Cancel();

            try
            {
                await Task.WhenAll(_sendTask, _receiveTask);
            }
            catch (OperationCanceledException) { }

            PCANBasic.Uninitialize(_handle);

            IsRunning = false;
        }
    }
}
