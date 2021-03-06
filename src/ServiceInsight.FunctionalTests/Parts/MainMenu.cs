﻿using TestStack.White.UIItems;
using TestStack.White.UIItems.Finders;
using TestStack.White.UIItems.WindowItems;
using TestStack.White.UIItems.WindowStripControls;
using TestStack.White.UIItems.WPFUIItems;

namespace NServiceBus.Profiler.FunctionalTests.Parts
{
    public class MainMenu : ProfilerElement
    {
        public MainMenu(Window mainWindow) : base(mainWindow)
        {
        }

        public MenuBar ToolsMenu
        {
            get { return GetMenu("ToolsMenu"); }
        }

        public MenuBar FileMenu
        {
            get { return GetMenu("FileMenu"); }
        }

        public MenuBar ViewMenu
        {
            get { return GetMenu("ViewMenu"); }
        }

        public MenuBar HelpMenu
        {
            get { return GetMenu("HelpMenu"); }
        }

        public Button CreateQueue
        {
            get { return GetMenuItem(ToolsMenu, "CreateQueueMenuItem"); }
        }

        public Button ConnectToServiceControl
        {
            get { return GetMenuItem(ToolsMenu, "ConnectToServiceControlMenuItem"); }
        }

        public GroupBox BarManager
        {
            get { return GetByAutomationId<GroupBox>("BarManager"); }
        }

        private MenuBar GetMenu(string name)
        {
            return BarManager.Get<MenuBar>(name);
        }

        private Button GetMenuItem(MenuBar menu, string name)
        {
            return menu.Get<Button>(SearchCriteria.ByAutomationId(name));
        }
    }
}