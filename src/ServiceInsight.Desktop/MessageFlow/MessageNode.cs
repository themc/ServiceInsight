﻿using System.Diagnostics;
using System.Windows;
using Mindscape.WpfDiagramming;
using NServiceBus.Profiler.Desktop.Models;
using System;

namespace NServiceBus.Profiler.Desktop.MessageFlow
{
    [DebuggerDisplay("Type={Message.FriendlyMessageType}, Id={Message.Id}")]
    public class MessageNode : DiagramNode
    {
        private const int heightNoEndpoints = 56;
        private const int endpointsHeight = 25;

        public MessageNode(IMessageFlowViewModel owner, StoredMessage message) 
        {
            IsResizable = false;
            Owner = owner;
            Bounds = new Rect(0, 0, 203, heightNoEndpoints);
            //ZOrder = 1;
            Data = message;
            ExceptionMessage = message.GetHeaderByKey(MessageHeaderKeys.ExceptionType);
        }

        public StoredMessage Message
        {
            get { return Data as StoredMessage; }
        }

        public IMessageFlowViewModel Owner
        {
            get; private set;
        }

        public void CopyConversationId()
        {
            Owner.CopyConversationId(Message);
        }

        public void CopyHeaders()
        {
            Owner.CopyMessageHeaders(Message);
        }

        public async void Retry()
        {
            await Owner.RetryMessage(Message);
            Message.Status = MessageStatus.RetryIssued;
            base.OnPropertyChanged("HasFailed");
            base.OnPropertyChanged("HasRetried");
        }

        public bool CanRetry()
        {
            return HasFailed;

        }

        public void ShowBody()
        {
            Owner.ShowMessageBody(Message);
        }

        public void ShowException()
        {
            Owner.ShowException(new ExceptionDetails(Message));
        }

        public void Refresh()
        {
        }

        public bool ShowEndpoints
        {
            get; set;
        }

        public void OnShowEndpointsChanged()
        {
            Bounds = new Rect(Bounds.Location, new Size(Bounds.Width, heightNoEndpoints + (ShowEndpoints ? endpointsHeight : 0)));
        }

        public bool ShowExceptionInfo
        {
            get { return !string.IsNullOrEmpty(ExceptionMessage); }
        }

        public string NSBVersion
        {
            get { return Message.GetHeaderByKey(MessageHeaderKeys.Version); }
        }

        public string SecondLevelRetries
        {
            get { return Message.GetHeaderByKey(MessageHeaderKeys.Retries); }
        }

        public bool IsPublished
        {
            get { return Message.MessageIntent == MessageIntent.Publish; }
        }

        public DateTime? TimeSent
        {
            get 
            { 
                var timeString = Message.GetHeaderByKey(MessageHeaderKeys.TimeSent);
                if (string.IsNullOrEmpty(timeString))
                    return null;
                return DateTime.ParseExact(timeString, HeaderInfo.MessageDateFormat, System.Globalization.CultureInfo.InvariantCulture); 
            }
        }

        public bool HasFailed
        {
            get
            {
                return Message.Status == MessageStatus.Failed ||
                       Message.Status == MessageStatus.RepeatedFailure;
            }
        }

        public bool HasRetried
        {
            get
            {
                return Message.Status == MessageStatus.RetryIssued;
            }
        }

        public string ExceptionMessage
        {
            get; set;
        }

        public bool IsCurrentMessage { get; set; }
    }
}