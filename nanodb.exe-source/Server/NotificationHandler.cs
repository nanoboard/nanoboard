using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using System.Timers;					//need to set and run timers, to make thread.Resume() on timerEvent method.

namespace NServer
{
    class NotificationHandler : IRequestHandler
    {
//			Run nanodb with argument "notif_mode0", "notif_mode1" or "notif_mode2" to set this param.
//		public static string mode = "0";		//0 - old mode,
//		public static string mode = "1";		//1 - wait HttpResponse from Wait_Notifications(),
		public static string mode = "2";		//2 - enable async-await, using IAsyncResult with BeginInvoke, and wait HttpResponse from async_method()
		
        public static NotificationHandler Instance;

        public Queue<string> Messages = new Queue<string>();

        public NotificationHandler(string set_mode = "0")			//create new object after request on /notif
        {
//			Console.WriteLine("set_mode: "+set_mode);
			mode = set_mode;
            Instance = this;					//create new Instance
			if(mode != "0"){
				SetTimer();							//and set timer, once.
//				SetTimer2();						//set test timer to add some message in Queue<string> Messages
			}
        }

        public HttpResponse Handle(HttpRequest request)
        {
			if(mode == "0"){														//if default mode, 
				if (Messages.Count > 0)												//and if notifications exists
				{
					return new HttpResponse(StatusCode.Ok, Messages.Dequeue());			//return notification
				}
				return new HttpResponse(StatusCode.Ok, "");							//else, return empty response.
			}
			else{
				try{
					if(threads.Count==0){																			//if number of threads is null
						threads.Add(null);																			//add one item in list, with value null
					}
			
					if(threads[threads.Count-1] != null){		//if last thread in list not null - this thread is still working.
						need_stop += 1;						//need to stop this.
					}

					lock(_lock){
//						Console.WriteLine("threads.Count: "+threads.Count+", threads2.Count: "+threads2.Count);
						Thread thread = new Thread(				//	in thic case, Thread.Sleep() stopping this thread, not all program stopping.
							() =>
							{
								if(HTTP_Response!=null){																			//if HTTP_Response already exists
									return;																							//just return.
								}
							
								if(use_async_await == false){
									HTTP_Response = Wait_Notifications(request);	// Publish the return value									//else, wait response from recursive function
								}else{
									HTTP_Response = async_method(request);	// Publish the return value									//else, wait response from recursive function
								}
							}
						);
					
						if(threads[threads.Count-1] == null){																						//if last thread is null
							threads[threads.Count-1] = thread;																						//this will be the current thread
						}else{
							threads.Add(thread);																									//else, add new thread in list.
						}

						(threads[threads.Count-1]).Start();											//start last thread
						(threads[threads.Count-1]).Join();											//return value from this thread.
						(threads[threads.Count-1]).Interrupt();										//after finish that thread, interrupt this.

						if(threads.Count>1){
							foreach(Thread th in threads.ToArray()){
								th.Abort();
							}
						}
	
						threads[threads.Count-1] = null;

						HttpResponse tempHTTP_Response = (HttpResponse)HTTP_Response;																			//take HTTP_Response to temp
						HTTP_Response = null;																							//set it as null
						return (HttpResponse)tempHTTP_Response;																			//and return tempHTTP_Response as response.
					}
				}catch (Exception ex){
					return new HttpResponse(StatusCode.Ok, "NotificationHandler.cs. Handle. ex: "+ex);
				}
			}
        }		

		//create timer1
		private static System.Timers.Timer aTimer;							//use this infinite timer to unsuspend threads by interval
		private int timeout_thread_sleep = 500; 							//timeout to sleep in thread, before check notifications, milliseconds
		private void SetTimer()												//Method to SetTimer. This method running once, and need to start timer, to resume threads, from sleeping
		{
//			Console.WriteLine("Set timer once");
			aTimer = new System.Timers.Timer(timeout_thread_sleep);			//will working every timeout_thread_sleep milliseconds.
			aTimer.Elapsed += OnTimedEvent;									//run this method, after timeout end.
			aTimer.AutoReset = true;										//try again, and again...
			aTimer.Enabled = true;											//enable timer

			//aTimer.Enabled = false;										//or disable
			//aTimer.Start();												//and then, enable by this way
			//aTimer.Stop();												//and disable, by this way...
		}
		private void OnTimedEvent(Object source, ElapsedEventArgs e)		//method to unsuspend last thread, when this is suspended.
		{
			List<Thread> thread_list = null;
			
			if(use_async_await == false){
				thread_list = threads;
			}else{
				thread_list = threads2;
			}
			
			if(thread_list.Count == 0){
				return;
			}
			if(thread_list[thread_list.Count-1] == null){							//if no any thread, or last thread == null
				return;																				//do nothing and return...
			}
			else{															//else if there is a thread
				if(																//but if
						(
							(thread_list[thread_list.Count-1]).ThreadState				//1
							&															//and
							ThreadState.Suspended										//1		==	1, else nulls.
						)
						==
						( ThreadState.Suspended )										//but this is not 1, this is ThreadState object with Suspended value.
				){												//so if thread is suspended (background this or not, does no matter)
					#pragma warning disable 0618
					thread_list[thread_list.Count-1].Resume();					//try to resume last thread, and disable warnings, about .Resume() is deprecated.
					#pragma warning restore 0618
				}
				
				
				
				
				
				
				
				
				
				
				return;															//now, just return;
			}
		}
		
		//create timer2 to add some messages in Queue<string> Messages
//		private static System.Timers.Timer aTimer2;							//use this infinite timer to unsuspend threads by interval
//		private void SetTimer2()												//Method to SetTimer. This method running once, and need to start timer, to resume threads, from sleeping
//		{
//			Console.WriteLine("Set timer2 once");
//			aTimer2 = new System.Timers.Timer(timeout_thread_sleep*5);			//will working every timeout_thread_sleep milliseconds.
//			aTimer2.Elapsed += OnTimedEvent2;									//run this method, after timeout end.
//			aTimer2.AutoReset = true;										//try again, and again...
//			aTimer2.Enabled = true;											//enable timer
//
//			//aTimer2.Enabled = false;										//or disable
//			//aTimer2.Start();												//and then, enable by this way
//			//aTimer2.Stop();												//and disable, by this way...
//		}
//		private int x = 0;
//		private void OnTimedEvent2(Object source, ElapsedEventArgs e)		//method to unsuspend last thread, when this is suspended.
//		{
//			Messages.Enqueue("test"+x);
//			x+=1;
//		}

		public object HTTP_Response = null; 								//This variable using to store HttpResponse, before return this value. This will be locked.

		List<Thread> threads = new List<Thread>();							//list of threads; Usually will contains only one current thread, as last thread.
		List<Thread> threads2 = new List<Thread>();							//list of threads, started on start long_time_method; Usually will contains only one current thread, as last thread.



//		public bool use_async_await = true;														//use async-await, using IAsyncResult with BeginInvoke or not? (true/false)
		public static bool use_async_await = ( (mode != "0" && mode == "2") ? true : false);	//use async-await, using IAsyncResult with BeginInvoke or not? (true/false)

		
		public object _lock = new object();		//lock all variables from modifications, where lock is used.
		
		
		//________________________________________________________________________________________________________________________
		//BEGIN EAP (Event-based Asynchronous Pattern) - async-await, using IAsyncResult with BeginInvoke
		public delegate HttpResponse ReturnHandler(HttpRequest request);
		
		public HttpResponse async_method(HttpRequest request)
		{
			ReturnHandler handler = new ReturnHandler(long_time_method);	//new thread started, in this case.
			try{
				IAsyncResult resultObj = handler.BeginInvoke((HttpRequest)request, new AsyncCallback(AsyncCompleted), "\nBeginInvoke: Асинхронные вызовы\n");
				HttpResponse res = handler.EndInvoke(resultObj);
				return res;
			}catch(Exception ex){
				Console.WriteLine("async_method. Exception: "+ex);
				return new HttpResponse(StatusCode.Ok, "empty_response4...");
			}
		}

		public HttpResponse long_time_method(HttpRequest request)			//after running this method from async_method, new thread is starting.
		{
			if(threads2.Count == 0){																						//if no any threads in List<Thread>threads2
				threads2.Add(System.Threading.Thread.CurrentThread);															//add current thread there, as first element.
			}else if(threads2[threads2.Count-1].ManagedThreadId != System.Threading.Thread.CurrentThread.ManagedThreadId){	//if there is already exists some thread.
				threads2.Add(System.Threading.Thread.CurrentThread);															//then add current thread in this list.
				
				foreach(Thread th in threads2.ToArray()){												//And for each old thread
					if(th.ManagedThreadId != System.Threading.Thread.CurrentThread.ManagedThreadId){		//which are not have id of the current thread
						th.Interrupt();																			//interrupt this thread
						th.Abort();																				//abort it
						threads2.Remove(th);																	//and remove this thread from List<Thread>threads2
					}
				}
			}

			HttpResponse result = Wait_Notifications(request);		//after this run in the current thread, this recursive method, to return HttpResponse.
			return result;									//and return result, when this result will be returned.
		}
		
		public void AsyncCompleted(IAsyncResult resObj)							//will be runned, when result returned.
		{
			string mes = (string)resObj.AsyncState;
//			Console.WriteLine(mes);
			return;
		}
		//END EAP (Event-based Asynchronous Pattern) - async-await, using IAsyncResult with BeginInvoke
		//________________________________________________________________________________________________________________________


		//This infinite-loop-method will wait notifications Messages, and return this when this will exists.
		public HttpResponse Wait_Notifications(HttpRequest request){
			while(true){
				if(Messages.Count > 0){											//if messages exists
					return new HttpResponse(StatusCode.Ok, Messages.Dequeue());		//just return it
				}else{															//else
					if(need_stop > 0){												//if need stop this thread, because new thread started
						need_stop -= 1;													//no need stop next thread, now
						return new HttpResponse(StatusCode.Ok, "");						//and just return empty_response2.
					}
				}
				
				#pragma warning disable 0618
				System.Threading.Thread.CurrentThread.Suspend();				//suspend the current thread, and wait to resume this by timer tick.
				#pragma warning restore 0618
			}
		}

		public int need_stop = 0;
		
    }
    
    //		Does no more than invoking predefined action and returning predefined reply to the client,
	//		when this action exists,
	//		or wait this action,
	//		and check is action exists in one thread.
	//		If second thread or more than 1 thread started, just return empty response there,
	//		and interrupt/abort this old threads, to leave one thread, as last and current.
}
