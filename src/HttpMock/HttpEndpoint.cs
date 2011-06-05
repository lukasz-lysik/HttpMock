﻿using System;
using System.Net;
using System.Threading;
using Kayak;
using Kayak.Http;

namespace HttpMock
{
	public interface IHttpEndpoint : IDisposable
	{
		IHttpEndpoint At(Uri uri);
		IHttpEndpoint WithNewContext();
		RequestHandler Stub(Func<RequestProcessor, RequestHandler> func);
	}

	public class HttpEndpoint : IHttpEndpoint
	{
		private Uri _applicationUri;
		private RequestProcessor _requestProcessor;
		
		private ISchedulerDelegate _schedulerDelegate;
		private KayakScheduler _scheduler;
		private Thread _thread;
		
		public KayakScheduler Scheduler {
			get { return _scheduler; }
		}


		public IHttpEndpoint At(Uri uri) {
			_schedulerDelegate = new SchedulerDelegate();
			_scheduler = new KayakScheduler(_schedulerDelegate);
			_requestProcessor = new RequestProcessor(uri);
			
			_applicationUri = uri;

			IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any,_applicationUri.Port);
			_scheduler.Post(() => 
				KayakServer.Factory
						.CreateHttp(_requestProcessor)
						.Listen(ipEndPoint));

			_thread = new Thread(_scheduler.Start);
			_thread.Start();
			return this;
		}
		public IHttpEndpoint WithNewContext() {
			_requestProcessor.ClearHandlers();
			return this;
		}

		public RequestHandler Stub(Func<RequestProcessor, RequestHandler> func)
		{
			return func.Invoke(_requestProcessor);
		}


		public void Dispose() {
			try
			{
				_thread.Abort();
			}
			catch (ThreadAbortException) {
			}
			_scheduler.Stop();
			_scheduler.Dispose();
		}
	}
}