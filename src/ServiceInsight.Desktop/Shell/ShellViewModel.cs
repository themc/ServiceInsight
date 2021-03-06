﻿using System;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Threading;
using Caliburn.PresentationFramework.ApplicationModel;
using Caliburn.PresentationFramework.Filters;
using Caliburn.PresentationFramework.Screens;
using NServiceBus.Profiler.Desktop.Core.Licensing;
using NServiceBus.Profiler.Desktop.Core.Settings;
using NServiceBus.Profiler.Desktop.Events;
using NServiceBus.Profiler.Desktop.Explorer;
using NServiceBus.Profiler.Desktop.Explorer.EndpointExplorer;
using NServiceBus.Profiler.Desktop.Explorer.QueueExplorer;
using NServiceBus.Profiler.Desktop.ExtensionMethods;
using NServiceBus.Profiler.Desktop.LogWindow;
using NServiceBus.Profiler.Desktop.MessageFlow;
using NServiceBus.Profiler.Desktop.MessageList;
using NServiceBus.Profiler.Desktop.MessageProperties;
using NServiceBus.Profiler.Desktop.MessageViewers;
using NServiceBus.Profiler.Desktop.Options;
using NServiceBus.Profiler.Desktop.ScreenManager;
using NServiceBus.Profiler.Desktop.Settings;
using NServiceBus.Profiler.Desktop.Startup;

namespace NServiceBus.Profiler.Desktop.Shell
{
    public class ShellViewModel : Conductor<IScreen>.Collection.AllActive, IShellViewModel
    {
        private readonly IAppCommands _appCommander;
        private readonly IScreenFactory _screenFactory;
        private readonly IWindowManagerEx _windowManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILicenseManager _licenseManager;
        private readonly ISettingsProvider _settingsProvider;
        private readonly ICommandLineArgParser _comandLineArgParser;
        private int _workCounter;
        private DispatcherTimer _refreshTimer;
        private DispatcherTimer _idleTimer;
        
        public const string UnlicensedStatusMessage = "Unlicensed version: {0} left";
        public const string LicensedStatusMessage = "Registered to '{0}'";

        public ShellViewModel(
            IAppCommands appCommander,
            IScreenFactory screenFactory,
            IWindowManagerEx windowManager,
            IQueueExplorerViewModel queueExplorer, 
            IEndpointExplorerViewModel endpointExplorer,
            IMessageListViewModel messages,
            IStatusBarManager statusBarManager,
            IEventAggregator eventAggregator,
            ILicenseManager licenseManager,
            IMessageFlowViewModel messageFlow,
            IMessageBodyViewModel messageBodyViewer,
            ISettingsProvider settingsProvider,
            IMessagePropertiesViewModel messageProperties,
            ILogWindowViewModel logWindow,
            ICommandLineArgParser comandLineArgParser)
        {
            _appCommander = appCommander;
            _screenFactory = screenFactory;
            _windowManager = windowManager;
            _eventAggregator = eventAggregator;
            _licenseManager = licenseManager;
            _settingsProvider = settingsProvider;
            _comandLineArgParser = comandLineArgParser;
            MessageProperties = messageProperties;
            MessageFlow = messageFlow;
            StatusBarManager = statusBarManager;
            QueueExplorer = queueExplorer;
            EndpointExplorer = endpointExplorer;
            MessageBody = messageBodyViewer;
            Messages = messages;
            LogWindow = logWindow;

            Items.Add(queueExplorer);
            Items.Add(endpointExplorer);
            Items.Add(messages);
            Items.Add(messageBodyViewer);
            Items.Add(messageFlow);

            InitializeAutoRefreshTimer();
            InitializeIdleTimer();
        }

        public override void AttachView(object view, object context)
        {
            base.AttachView(view, context);
            View = (IShellView)view;

            DisplayName = GetProductName();
            StatusBarManager.Done();
            RestoreLayout();
        }

        public virtual void Deactivate(bool close )
        {
            base.OnDeactivate(close);
            _refreshTimer.Stop();
            SaveLayout();
        }

        private void SaveLayout()
        {
            View.OnSaveLayout(_settingsProvider);
        }

        private void RestoreLayout()
        {
            View.OnRestoreLayout(_settingsProvider);
        }

        public virtual void ResetLayout()
        {
            View.OnResetLayout(_settingsProvider);
        }

        public virtual bool AutoRefresh { get; set; }

        public virtual IShellView View { get; private set; }

        public virtual IMessagePropertiesViewModel MessageProperties { get; private set; }

        public virtual IQueueExplorerViewModel QueueExplorer { get; private set; }

        public virtual IEndpointExplorerViewModel EndpointExplorer { get; private set; }

        public virtual IMessageListViewModel Messages { get; private set; }

        public virtual IMessageFlowViewModel MessageFlow { get; private set; }

        public virtual IMessageBodyViewModel MessageBody { get; private set; }

        public virtual IStatusBarManager StatusBarManager { get; private set; }

        public virtual ILogWindowViewModel LogWindow { get; private set; }

        public virtual ExplorerItem SelectedExplorerItem { get; private set; }

        public virtual bool WorkInProgress
        {
            get { return _workCounter > 0; }
        }

        public virtual void ShutDown()
        {
            _appCommander.ShutdownImmediately();
        }

        public virtual void About()
        {
            _windowManager.ShowDialog<AboutViewModel>();
        }

        public virtual void Help()
        {
            throw new NotImplementedException("This feature is not yet implemented.");
        }

        public void Options()
        {
            _windowManager.ShowDialog<OptionsViewModel>();
        }

        [AutoCheckAvailability]
        public void ConnectToMessageQueue()
        {
            var machineViewModel = _screenFactory.CreateScreen<IConnectToMachineViewModel>();
            var result = _windowManager.ShowDialog(machineViewModel);

            if(result.GetValueOrDefault(false))
            {
                QueueExplorer.ConnectToQueue(machineViewModel.ComputerName);
            }
        }

        [AutoCheckAvailability]
        public async void ConnectToServiceControl()
        {
            var connectionViewModel = _screenFactory.CreateScreen<ServiceControlConnectionViewModel>();
            var result = _windowManager.ShowDialog(connectionViewModel);

            if (result.GetValueOrDefault(false))
            {
                await EndpointExplorer.ConnectToService(connectionViewModel.ServiceUrl);
                _eventAggregator.Publish(new WorkFinished("Connected to ServiceControl Version {0}", connectionViewModel.Version));
            }
        }

        [AutoCheckAvailability]
        public async void DeleteSelectedMessages()
        {
            await Messages.DeleteSelectedMessages();
        }

        [AutoCheckAvailability]
        public async void PurgeCurrentQueue()
        {
            await Messages.PurgeQueue();
        }

        [AutoCheckAvailability]
        public void DeleteCurrentQueue()
        {
            QueueExplorer.DeleteSelectedQueue();
        }

        [AutoCheckAvailability]
        public async void RefreshAll()
        {
            await EndpointExplorer.RefreshData();
            await QueueExplorer.RefreshData();
            await Messages.RefreshMessages();
        }

        [AutoCheckAvailability]
        public void ImportMessage()
        {
            throw new NotImplementedException("This feature is not yet implemented.");
        }

        [AutoCheckAvailability]
        public void ExportMessage()
        {
            throw new NotImplementedException("This feature is not yet implemented.");
        }

        [AutoCheckAvailability]
        public async Task CreateQueue()
        {
            var screen = _screenFactory.CreateScreen<IQueueCreationViewModel>();
            var result = _windowManager.ShowDialog(screen);

            if(result.GetValueOrDefault(false))
            {
                await QueueExplorer.RefreshData();
            }
        }

        [AutoCheckAvailability]
        public void CreateMessage()
        {
            throw new NotImplementedException("This feature is not yet implemented.");
        }

        public void Register()
        {
            _windowManager.ShowDialog<ILicenseRegistrationViewModel>();
            DisplayRegistrationStatus();
        }

        public void OnAutoRefreshChanged()
        {
            _refreshTimer.IsEnabled = AutoRefresh;
        }

        public bool CanCreateMessage
        {
            get { return QueueExplorer.SelectedQueue != null && !WorkInProgress; }
        }

        public bool CanRefreshQueues
        {
            get { return !WorkInProgress; }
        }

        public bool CanPurgeCurrentQueue
        {
            get
            {
                return Messages.SelectedQueue != null &&
                       !WorkInProgress &&
                       SelectedExplorerItem.IsQueueExplorerSelected();
            }
        }

        public bool CanDeleteCurrentQueue
        {
            get
            {
                return Messages.SelectedQueue != null &&
                       !WorkInProgress &&
                       SelectedExplorerItem.IsQueueExplorerSelected();
            }
        }

        public bool CanDeleteSelectedMessages
        {
            get
            {
                return !WorkInProgress &&
                       Messages.FocusedMessage != null &&
                       SelectedExplorerItem.IsQueueExplorerSelected();
            }
        }

        public bool CanCreateQueue
        {
            get
            {
                return !QueueExplorer.ConnectedToAddress.IsEmpty() &&
                       !WorkInProgress &&
                       SelectedExplorerItem.IsQueueExplorerSelected();
            }
        }

        public int SelectedMessageTabItem { get; set; }

        public bool CanConnectToMachine
        {
            get { return !WorkInProgress || AutoRefresh; }
        }

        public bool CanConnectToServiceControl
        {
            get { return !WorkInProgress || AutoRefresh; }
        }

        public bool CanExportMessage
        {
            get { return !WorkInProgress && Messages.SelectedMessages.Count > 0 && false; } //TODO: Implement message export
        }

        public bool CanImportMessage
        {
            get { return !WorkInProgress && false; } //TODO: Implement message import
        }
        
        private void InitializeIdleTimer()
        {
            _idleTimer = new DispatcherTimer(DispatcherPriority.Loaded) {Interval = TimeSpan.FromSeconds(10)};
            _idleTimer.Tick += (s, e) => OnApplicationIdle();
            _idleTimer.Start();
        }

        private void InitializeAutoRefreshTimer()
        {
            var appSetting = _settingsProvider.GetSettings<ProfilerSettings>();
            var startupTime = _comandLineArgParser.ParsedOptions.ShouldAutoRefresh ? _comandLineArgParser.ParsedOptions.AutoRefreshRate : appSetting.AutoRefreshTimer;

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(startupTime) };
            _refreshTimer.Tick += (s, e) => OnAutoRefreshing();

            AutoRefresh = _comandLineArgParser.ParsedOptions.ShouldAutoRefresh;
        }

        internal void OnApplicationIdle()
        {
            if (_idleTimer != null)
                _idleTimer.Stop();

            ValidateLicense();
        }

        internal void OnAutoRefreshing()
        {
            if (!AutoRefresh || WorkInProgress)
                return;

            RefreshAll();
        }
        
        private void ValidateLicense()
        {
            if (_licenseManager.TrialExpired)
            {
                RegisterLicense();
            }

            DisplayRegistrationStatus();
        }

        private void DisplayRegistrationStatus()
        {
            var license = _licenseManager.CurrentLicense;

            if (license == null)
            {
                return;
            }
            if (license.LicenseType == ProfilerLicenseTypes.Standard)
            {
                StatusBarManager.SetRegistrationInfo(LicensedStatusMessage, license.RegisteredTo);
            }
            else
            {
                StatusBarManager.SetRegistrationInfo(UnlicensedStatusMessage, ("day").PluralizeWord(_licenseManager.GetRemainingTrialDays()));
            }
        }

        private void RegisterLicense()
        {
            var model = _screenFactory.CreateScreen<ILicenseRegistrationViewModel>();
            var result = _windowManager.ShowDialog(model);

            if (!result.GetValueOrDefault(false))
            {
                ShutDown();
            }
        }

        private void NotifyPropertiesChanged()
        {
            NotifyOfPropertyChange(() => WorkInProgress);
            NotifyOfPropertyChange(() => CanConnectToMachine);
            NotifyOfPropertyChange(() => CanConnectToServiceControl);
            NotifyOfPropertyChange(() => CanCreateMessage);
            NotifyOfPropertyChange(() => CanCreateQueue);
            NotifyOfPropertyChange(() => CanDeleteCurrentQueue);
            NotifyOfPropertyChange(() => CanDeleteSelectedMessages);
            NotifyOfPropertyChange(() => CanExportMessage);
            NotifyOfPropertyChange(() => CanImportMessage);
            NotifyOfPropertyChange(() => CanPurgeCurrentQueue);
            NotifyOfPropertyChange(() => CanRefreshQueues);
        }

        private string GetProductName()
        {
            var productAttribute = GetType().Assembly.GetAttribute<AssemblyProductAttribute>();
            return productAttribute.Product;
        }

        public void Handle(WorkStarted @event)
        {
            _workCounter++;
            NotifyPropertiesChanged();
        }

        public void Handle(WorkFinished @event)
        {
            if (_workCounter <= 0) 
                return;

            _workCounter--;
            NotifyPropertiesChanged();
        }

        public void Handle(SelectedExplorerItemChanged @event)
        {
            SelectedExplorerItem = @event.SelectedExplorerItem;
        }

        public void Handle(SwitchToMessageBody @event)
        {
            View.SelectTab("MessageBody");
        }
    }
}