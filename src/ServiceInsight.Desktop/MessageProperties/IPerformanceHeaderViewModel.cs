﻿using System;

namespace NServiceBus.Profiler.Desktop.MessageProperties
{
    public interface IPerformanceHeaderViewModel : IHeaderInfoViewModel, IPropertyDataProvider
    {
        DateTime? TimeSent { get; }
        DateTime? ProcessingStarted { get; }
        DateTime? ProcessingEnded { get; }
        string ProcessingTime { get; }
        string DeliveryTime { get; }
        string CriticalTime { get; }
    }
}