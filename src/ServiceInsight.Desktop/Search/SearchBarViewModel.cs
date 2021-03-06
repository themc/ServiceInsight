﻿using System;
using System.Collections.Generic;
using Caliburn.PresentationFramework.Filters;
using Caliburn.PresentationFramework.Screens;
using NServiceBus.Profiler.Desktop.Events;
using NServiceBus.Profiler.Desktop.Explorer.EndpointExplorer;
using NServiceBus.Profiler.Desktop.Explorer.QueueExplorer;
using NServiceBus.Profiler.Desktop.ExtensionMethods;
using NServiceBus.Profiler.Desktop.MessageList;
using NServiceBus.Profiler.Desktop.Explorer;
using NServiceBus.Profiler.Desktop.Models;
using NServiceBus.Profiler.Desktop.Startup;

namespace NServiceBus.Profiler.Desktop.Search
{
    public class SearchBarViewModel : Screen, ISearchBarViewModel
    {
        private readonly ICommandLineArgParser _commandLineArgParser;
        private int _workCount;

        public SearchBarViewModel(ICommandLineArgParser commandLineArgParser)
        {
            _commandLineArgParser = commandLineArgParser;
            PageSize = 50; //NOTE: Do we need to change this?
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            SearchQuery = _commandLineArgParser.ParsedOptions.SearchQuery;
            SearchInProgress = !SearchQuery.IsEmpty();
            SearchEnabled = !SearchQuery.IsEmpty();
            NotifyPropertiesChanged();
        }

        public void GoToFirstPage()
        {
            Parent.RefreshEndpoint(SelectedEndpoint, 1, SearchQuery);
        }

        public void GoToPreviousPage()
        {
            Parent.RefreshEndpoint(SelectedEndpoint, CurrentPage - 1, SearchQuery);
        }

        public void GoToNextPage()
        {
            Parent.RefreshEndpoint(SelectedEndpoint, CurrentPage + 1, SearchQuery);
        }

        public void GoToLastPage()
        {
            Parent.RefreshEndpoint(SelectedEndpoint, PageCount, SearchQuery);
        }

        [AutoCheckAvailability]
        public async void Search()
        {
            SearchInProgress = true;
            await Parent.RefreshEndpoint(SelectedEndpoint, 1, SearchQuery);
        }

        [AutoCheckAvailability]
        public async void CancelSearch()
        {
            SearchQuery = null;
            SearchInProgress = false;
            await Parent.RefreshEndpoint(SelectedEndpoint, 1, SearchQuery);
        }

        public void SetupPaging(PagedResult<MessageInfo> pagedResult)
        {
            Result = pagedResult.Result;
            CurrentPage = pagedResult.TotalCount > 0 ? pagedResult.CurrentPage : 0;
            TotalItemCount = pagedResult.TotalCount;

            NotifyPropertiesChanged();
        }

        public async void RefreshResult()
        {
            if (SelectedEndpoint != null)
            {
                await Parent.RefreshEndpoint(SelectedEndpoint, CurrentPage, SearchQuery);
            }
            else
            {
                await Parent.RefreshMessages();
            }
        }

        public bool CanGoToLastPage
        {
            get
            {
                return SelectedEndpoint != null &&
                       CurrentPage < PageCount &&
                       !WorkInProgress;
            }
        }

        public bool CanCancelSearch
        {
            get { return SearchInProgress; }
        }

        public new IMessageListViewModel Parent
        {
            get { return base.Parent as IMessageListViewModel; }
        }
        
        public int PageCount
        {
            get
            {
                if (TotalItemCount == 0)
                {
                    return 0;
                }

                return (int)Math.Ceiling((double)TotalItemCount / PageSize);
            }
        }

        public bool WorkInProgress
        {
            get { return _workCount > 0; }
        }

        public Endpoint SelectedEndpoint { get; private set; }

        public Queue SelectedQueue { get; private set; }
        
        public string SearchQuery { get; set; }

        public bool IsVisible { get; set; }

        public bool CanGoToFirstPage
        {
            get
            {
                return SelectedEndpoint != null &&
                       CurrentPage > 1 &&
                       !WorkInProgress;
            }
        }

        public bool CanGoToPreviousPage
        {
            get
            {
                return SelectedEndpoint != null &&
                       CurrentPage - 1 >= 1 &&
                       !WorkInProgress;
            }
        }

        public bool CanGoToNextPage
        {
            get
            {
                return SelectedEndpoint != null &&
                       CurrentPage + 1 <= PageCount &&
                       !WorkInProgress;
            }
        }

        public IList<MessageInfo> Result { get; private set; }

        public int CurrentPage { get; private set; }
        
        public int PageSize { get; private set; }
        
        public int TotalItemCount { get; private set; }
        
        public bool SearchInProgress { get; private set; }
        
        public bool SearchEnabled { get; private set; }

        public bool CanSearch
        {
            get
            {
                return !WorkInProgress &&
                       !string.IsNullOrWhiteSpace(SearchQuery) &&
                       SelectedEndpoint != null;
            }
        }

        public bool CanRefreshResult
        {
            get
            {
                return !WorkInProgress && (SelectedEndpoint != null || SelectedQueue != null);
            }
        }

        public void NotifyPropertiesChanged()
        {
            NotifyOfPropertyChange(() => PageCount);
            NotifyOfPropertyChange(() => CanGoToFirstPage);
            NotifyOfPropertyChange(() => CanGoToLastPage);
            NotifyOfPropertyChange(() => CanGoToNextPage);
            NotifyOfPropertyChange(() => CanGoToPreviousPage);
            NotifyOfPropertyChange(() => CanRefreshResult);
            NotifyOfPropertyChange(() => SearchEnabled);
            NotifyOfPropertyChange(() => CanCancelSearch);
            NotifyOfPropertyChange(() => WorkInProgress);
        }

        public void OnSelectedEndpointChanged()
        {
            if (SelectedEndpoint != null)
            {
                SelectedQueue = null;
                SearchEnabled = true;
            }
        }

        public void OnSelectedQueueChanged()
        {
            if (SelectedQueue != null)
            {
                SelectedEndpoint = null;
                SearchEnabled = false;
            }
        }

        public virtual void Handle(SelectedExplorerItemChanged @event)
        {
            var endpointNode = @event.SelectedExplorerItem.As<EndpointExplorerItem>();
            if (endpointNode != null)
            {
                SelectedEndpoint = endpointNode.Endpoint;                
            }

            var queueNode = @event.SelectedExplorerItem.As<QueueExplorerItem>();
            if (queueNode != null)
            {
                SelectedQueue = queueNode.Queue;
            }

            NotifyPropertiesChanged();
        }

        public void Handle(WorkStarted @event)
        {
            _workCount++;
            NotifyPropertiesChanged();
        }

        public void Handle(WorkFinished @event)
        {
            if (_workCount > 0)
            {
                _workCount--;
                NotifyPropertiesChanged();
            }
        }
    }
}