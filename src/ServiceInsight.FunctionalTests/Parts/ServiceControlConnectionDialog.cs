﻿using TestStack.White.UIItems;
using TestStack.White.UIItems.Finders;
using TestStack.White.UIItems.ListBoxItems;
using TestStack.White.UIItems.WindowItems;

namespace NServiceBus.Profiler.FunctionalTests.Parts
{
    public class ServiceControlConnectionDialog : ProfilerElement
    {
        private Window dialog;

        public ServiceControlConnectionDialog(Window mainWindow) : base(mainWindow)
        {
        }

        public void Activate()
        {
            dialog = MainWindow.ModalWindow(SearchCriteria.ByAutomationId("ServiceControlConnectionDialog"));
        }

        public ComboBox ServiceUrl
        {
            get { return dialog.Get<ComboBox>("ServiceUrl"); }
        }

        public Button Okay
        {
            get { return dialog.Get<Button>(SearchCriteria.ByAutomationId("OK")); }
        }

        public Button Cancel
        {
            get { return dialog.Get<Button>(SearchCriteria.ByAutomationId("Cancel")); }
        }

        public bool IsClosed
        {
            get { return dialog.IsClosed; }
        }
    }
}