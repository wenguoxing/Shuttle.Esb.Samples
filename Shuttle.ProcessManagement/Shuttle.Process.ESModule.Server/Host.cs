﻿using System;
using Castle.Windsor;
using log4net;
using Shuttle.Castle;
using Shuttle.Core.Castle;
using Shuttle.Core.Data;
using Shuttle.Core.Host;
using Shuttle.Core.Infrastructure;
using Shuttle.Core.Log4Net;
using Shuttle.EMailSender.Messages;
using Shuttle.Esb;
using Shuttle.Esb.Castle;
using Shuttle.Esb.Msmq;
using Shuttle.Esb.Process;
using Shuttle.Esb.Sql;
using Shuttle.Invoicing.Messages;
using Shuttle.Ordering.Messages;
using Shuttle.Recall;
using Shuttle.Recall.Sql;

namespace Shuttle.Process.ESModule.Server
{
	public class Host : IHost, IDisposable
	{
		private IServiceBus _bus;
		private IWindsorContainer _container;

		public void Dispose()
		{
			if (_bus != null)
			{
				_bus.Dispose();
			}

			if (_container != null)
			{
				_container.Dispose();
			}
		}

		public void Start()
		{
			Log.Assign(new Log4NetLog(LogManager.GetLogger(typeof(Host))));

			_container = new WindsorContainer();

			_container.RegisterDataAccessCore();
			_container.RegisterDataAccess("Shuttle.ProcessManagement");

			var container = new WindsorComponentContainer(_container);

			container.Register<Recall.Sql.IScriptProviderConfiguration, Recall.Sql.ScriptProviderConfiguration>();
			container.Register<Recall.Sql.IScriptProvider, Recall.Sql.ScriptProvider>();

			container.Register<IProjectionRepository, ProjectionRepository>();
			container.Register<IProjectionQueryFactory, ProjectionQueryFactory>();
			container.Register<IPrimitiveEventRepository, PrimitiveEventRepository>();
			container.Register<IPrimitiveEventQueryFactory, PrimitiveEventQueryFactory>();
			container.Register<IKeyStoreQueryFactory, KeyStoreQueryFactory>();
			container.Register<IKeyStore, KeyStore>();

			container.Register<IProjectionConfiguration>(ProjectionSection.Configuration());

			EventProcessingModule.RegisterComponents(container);
			EventStore.RegisterComponents(container);

			container.Register<IProcessConfiguration>(ProcessSection.Configuration());
			container.Register<IProcessActivator, DefaultProcessActivator>();
			container.Register<IMessageHandlerInvoker, ProcessMessageHandlerInvoker>();

			container.Register<IMsmqConfiguration, MsmqConfiguration>();

			container.Register<Esb.Sql.IScriptProviderConfiguration, Esb.Sql.ScriptProviderConfiguration>();
			container.Register<Esb.Sql.IScriptProvider, Esb.Sql.ScriptProvider>();

			container.Register<ISqlConfiguration>(SqlSection.Configuration());
			container.Register<ISubscriptionManager, SubscriptionManager>();

			ServiceBus.RegisterComponents(container);

			container.Resolve<EventProcessingModule>();

			var processActivator = container.Resolve<IProcessActivator>() as DefaultProcessActivator;

			if (processActivator != null)
			{
				processActivator.RegisterMappings();
			}

			var subscriptionManager = container.Resolve<ISubscriptionManager>();

			subscriptionManager.Subscribe<OrderCreatedEvent>();
			subscriptionManager.Subscribe<InvoiceCreatedEvent>();
			subscriptionManager.Subscribe<EMailSentEvent>();

			_bus = ServiceBus.Create(container).Start();
		}
	}
}