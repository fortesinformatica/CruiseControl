﻿namespace ThoughtWorks.CruiseControl.UnitTests.WebDashboard.Plugins.ProjectReport
{
    using NUnit.Framework;
    using Rhino.Mocks;
    using ThoughtWorks.CruiseControl.WebDashboard.Dashboard;
    using ThoughtWorks.CruiseControl.WebDashboard.MVC.Cruise;
    using ThoughtWorks.CruiseControl.WebDashboard.Plugins.ProjectReport;

    public class ProjectTimelinePluginTests
    {
        #region Private fields
        private MockRepository mocks;
        #endregion

        #region Setup
        [SetUp]
        public void Setup()
        {
            this.mocks = new MockRepository();
        }
        #endregion

        #region Tests
        [Test]
        public void DescriptionIsCorrect()
        {
            var plugin = new ProjectTimelinePlugin(null);
            Assert.AreEqual("Project Timeline", plugin.LinkDescription);
        }

        [Test]
        public void NamedActionsReturnsBothActions()
        {
            var actionInstantiator = this.mocks.StrictMock<IActionInstantiator>();
            var action = this.mocks.StrictMock<ICruiseAction>();
            SetupResult.For(actionInstantiator.InstantiateAction(typeof(ProjectTimelineAction)))
                .Return(action);

            this.mocks.ReplayAll();
            var plugin = new ProjectTimelinePlugin(actionInstantiator);
            var actions = plugin.NamedActions;

            this.mocks.VerifyAll();
            Assert.AreEqual(2, actions.Length);
            Assert.IsInstanceOf<ImmutableNamedAction>(actions[0]);
            Assert.AreEqual(ProjectTimelineAction.TimelineActionName, actions[0].ActionName);
            Assert.AreSame(action, actions[0].Action);
            Assert.IsInstanceOf<ImmutableNamedAction>(actions[1]);
            Assert.AreEqual(ProjectTimelineAction.DataActionName, actions[1].ActionName);
            Assert.AreSame(action, actions[1].Action);
        }
        #endregion
    }
}
