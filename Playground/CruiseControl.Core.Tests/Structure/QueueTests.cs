﻿namespace CruiseControl.Core.Tests.Structure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using CruiseControl.Core.Interfaces;
    using CruiseControl.Core.Structure;
    using CruiseControl.Core.Tests.Stubs;
    using Moq;
    using NUnit.Framework;

    public class QueueTests
    {
        #region Tests
        [Test]
        public void AskToIntegrateWillTriggerProjectIfFirst()
        {
            var project = new ProjectStub();
            var queue = new Queue();
            var context = new IntegrationContext(project);
            queue.AskToIntegrate(context);
            var canIntegrate = context.Wait(TimeSpan.FromSeconds(5));
            Assert.IsTrue(canIntegrate);
            var active = queue.GetActiveRequests();
            Assert.AreEqual(1, active.Count());
            Assert.AreEqual(0, queue.GetPendingRequests().Count());
            Assert.AreSame(active.First(), context);
        }

        [Test]
        public void AskToIntegrateFailsIfUnableToLock()
        {
            var project = new ProjectStub();
            var queue = new TestQueue();
            var context = new IntegrationContext(project);
            queue.Lock();
            Assert.Throws<Exception>(() => queue.AskToIntegrate(context));
        }

        [Test]
        public void CompletingAnIntegrationRemovesItFromActiveRequests()
        {
            var project = new ProjectStub();
            var queue = new Queue();
            var context = new IntegrationContext(project);
            queue.AskToIntegrate(context);
            context.Complete();
            Assert.AreEqual(0, queue.GetActiveRequests().Count());
            Assert.AreEqual(0, queue.GetPendingRequests().Count());
        }

        [Test]
        public void CompletingAnIntegrationFailsIfUnableToLock()
        {
            var project = new ProjectStub();
            var queue = new TestQueue();
            var context = new IntegrationContext(project);
            queue.AskToIntegrate(context);
            queue.Lock();
            Assert.Throws<Exception>(context.Complete);
        }

        [Test]
        public void AskToIntegrateWillQueueSubsequentItems()
        {
            var queue = new Queue();
            var project1 = new ProjectStub();
            var project2 = new ProjectStub();
            var context1 = new IntegrationContext(project1);
            var context2 = new IntegrationContext(project2);
            queue.AskToIntegrate(context1);
            queue.AskToIntegrate(context2);
            var active = queue.GetActiveRequests();
            var pending = queue.GetPendingRequests();
            Assert.AreEqual(1, active.Count());
            Assert.AreEqual(1, pending.Count());
            Assert.AreSame(active.First(), context1);
            Assert.AreSame(pending.First(), context2);
        }

        [Test]
        public void CompletingReleasingSubsequentItems()
        {
            var queue = new Queue();
            var project1 = new ProjectStub();
            var project2 = new ProjectStub();
            var context1 = new IntegrationContext(project1);
            var context2 = new IntegrationContext(project2);
            queue.AskToIntegrate(context1);
            queue.AskToIntegrate(context2);
            context1.Complete();
            var active = queue.GetActiveRequests();
            Assert.AreEqual(1, active.Count());
            Assert.AreEqual(0, queue.GetPendingRequests().Count());
            Assert.AreSame(context2, active.First());
        }

        [Test]
        public void GetPendingRequestsFailsIfUnableToLock()
        {
            var queue = new TestQueue();
            queue.Lock();
            Assert.Throws<Exception>(() => queue.GetPendingRequests());
        }

        [Test]
        public void GetActiveRequestsFailsIfUnableToLock()
        {
            var queue = new TestQueue();
            queue.Lock();
            Assert.Throws<Exception>(() => queue.GetActiveRequests());
        }

        [Test]
        public void ItemTypeIsQueue()
        {
            var item = new Queue();
            Assert.AreEqual("Queue", item.ItemType);
        }

        [Test]
        public void AddingAProjectSetsItsHostToTheQueue()
        {
            var queue = new Queue();
            var project = new Project();
            queue.Children.Add(project);
            Assert.AreSame(queue, project.Host);
        }

        [Test]
        public void AddingAQueueSetsItsHostToTheQueue()
        {
            var queue1 = new Queue();
            var queue2 = new Queue();
            queue1.Children.Add(queue2);
            Assert.AreSame(queue1, queue2.Host);
        }

        [Test]
        public void StartingAQueueWithAProjectSetsTheHost()
        {
            var project = new Project();
            var queue = new Queue("Test", project);
            Assert.AreSame(queue, project.Host);
        }

        [Test]
        public void HandlesQueueOfQueues()
        {
            var integrations = new List<string>();
            var contexts = new List<IntegrationContext>();
            var projects = new Project[6];
            for (var loop = 0; loop < projects.Length; loop++)
            {
                var project = new Project("Project" + loop);
                projects[loop] = project;
            }

            var queue1 = new Queue("Queue1", projects[0], projects[1]);
            var queue2 = new Queue("Queue2", projects[2], projects[3]);
            var queue3 = new Queue("Queue3", projects[4], projects[5]);
            var queue4 = new Queue("Queue4", queue1, queue2, queue3)
                             {
                                 AllowedActive = 2
                             };

            // Trying to simulate async code here - need to fire the completion events in the
            // order they are released
            var completed = new List<IntegrationContext>();
            foreach (var project in projects)
            {
                var context = new IntegrationContext(project);
                contexts.Add(context);
                project.Host.AskToIntegrate(context);
                if (context.IsLocked)
                {
                    context.Released += (o, e) =>
                                            {
                                                var subContext = o as IntegrationContext;
                                                completed.Add(subContext);
                                                integrations.Add(subContext.Item.Name);
                                            };
                }
                else
                {
                    completed.Add(context);
                    integrations.Add(project.Name);
                }
            }

            // completed will be modified at the same time we are iterating through it so can't
            // use foreach here
            for (var loop = 0; loop < completed.Count; loop++)
            {
                completed[loop].Complete();
            }

            // These should both be empty at the end of the process
            Assert.AreEqual(0, queue4.GetActiveRequests().Count());
            Assert.AreEqual(0, queue4.GetPendingRequests().Count());

            // Check that there is the correct order
            var expected = new[]
                               {
                                   "Project0",
                                   "Project2",
                                   "Project4",
                                   "Project1",
                                   "Project3",
                                   "Project5"
                               };
            CollectionAssert.AreEqual(expected, integrations);
        }

        [Test]
        public void ValidateValidatesChildren()
        {
            var validated = false;
            var projectStub = new ProjectStub
                                  {
                                      OnValidate = vl => validated = true
                                  };
            var validationMock = new Mock<IValidationLog>();
            var queue = new Queue("Test", projectStub);
            queue.Validate(validationMock.Object);
            Assert.IsTrue(validated);
        }

        [Test]
        public void RemovingAChildResetsItsParent()
        {
            var project = new Project();
            var queue = new Queue("Test", project);
            queue.Children.Remove(project);
            Assert.IsNull(project.Host);
        }

        [Test]
        public void UniversalNameHandlesTopLevel()
        {
            var queue = new Queue("TestQueue");
            new Server(queue)
                {
                    Name = "ServerName"
                };
            var actual = queue.UniversalName;
            Assert.AreEqual("urn:ccnet:ServerName:TestQueue", actual);
        }

        [Test]
        public void UniversalNameHandlesChildItem()
        {
            var child = new Queue("ChildQueue");
            var parent = new Queue("ParentQueue", child);
            new Server(parent)
                {
                    Name = "ServerName"
                };
            var actual = child.UniversalName;
            Assert.AreEqual("urn:ccnet:ServerName:ParentQueue:ChildQueue", actual);
        }

        [Test]
        public void ValidateDetectsDuplicateChildItems()
        {
            var errorAdded = false;
            var project1 = new Project("Project");
            var project2 = new Project("Project");
            var project3 = new Project("OtherProject");
            var queue = new Queue("QueueName", project1, project2, project3);
            var validationStub = new ValidationLogStub
                                     {
                                         OnAddErrorMessage = (m, a) =>
                                                                 {
                                                                     Assert.AreEqual(
                                                                         "Duplicate {1} name detected: '{0}'", m);
                                                                     CollectionAssert.AreEqual(
                                                                         new[] { "Project", "child" },
                                                                         a);
                                                                     errorAdded = true;
                                                                 }
                                     };
            queue.Validate(validationStub);
            Assert.IsTrue(errorAdded);
        }

        [Test]
        public void ListProjectsReturnsOnlyTheProjects()
        {
            var project = new Project();
            var childQueue = new Queue();
            var parentQueue = new Queue("Test", project, childQueue);
            var projects = parentQueue.ListProjects();
            var expected = new[] { project };
            CollectionAssert.AreEqual(expected, projects);
        }

        [Test]
        public void LocateMatchesSelf()
        {
            var childQueue = new Queue("ChildQueue");
            var queue = new Queue("RootQueue", childQueue);
            var server = new Server("Local", queue);
            var actual = server.Locate("urn:ccnet:local:rootQueue");
            Assert.AreSame(queue, actual);
        }

        [Test]
        public void LocateMatchesChild()
        {
            var childQueue = new Queue("ChildQueue");
            var queue = new Queue("RootQueue", childQueue);
            var server = new Server("Local", queue);
            var actual = server.Locate("urn:ccnet:local:rootQueue:childQueue");
            Assert.AreSame(childQueue, actual);
        }

        [Test]
        public void GetPriorityReturnsNullIfNoPrioritySet()
        {
            var project = new Project();
            var actual = Queue.GetPriority(project);
            Assert.IsNull(actual);
        }

        [Test]
        public void SetPrioritySetsPriority()
        {
            var project = new Project();
            Queue.SetPriority(project, 1);
            var actual = Queue.GetPriority(project);
            Assert.AreEqual(1, actual);
        }

        [Test]
        public void SetPriorityClearsPriority()
        {
            var project = new Project();
            Queue.SetPriority(project, 1);
            Queue.SetPriority(project, null);
            var actual = Queue.GetPriority(project);
            Assert.IsNull(actual);
        }
        #endregion

        #region Classes
        public class TestQueue
            : Queue
        {
            public void Lock()
            {
                var handle = new ManualResetEvent(false);
                var thread = new Thread(() =>
                                            {
                                                this.Interleave.TryEnterWriteLock(TimeSpan.FromSeconds(5));
                                                handle.Set();
                                            });
                thread.Start();
                handle.WaitOne();
            }
        }
        #endregion
    }
}
