﻿using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Silk.Core.Types;
using Assert = Xunit.Assert;

namespace Silk.Core.Tests.Types
{
	public class AsyncTimerTest
	{
		[Test]
		public void AsyncTimer_ExecutesTask()
		{
			//Arrange
			var executed = false;
			var timer = new AsyncTimer(() =>
			{
				executed = true;
				return Task.CompletedTask;
			}, TimeSpan.FromSeconds(1));
			
			//Act
			timer.Start();
			timer.Stop();

			//Assert
			Assert.True(executed);
		}
		
		[Test]
		public void AsyncTimer_Executes_Task_WithParameters()
		{
			//Arrange
			var executed = false;

			var timer = new AsyncTimer(_ =>
			{
				executed = true;
				return Task.CompletedTask;
			}, null!, TimeSpan.FromSeconds(1));
			
			//Act
			timer.Start();
			timer.Stop();
			
			//Assert
			Assert.True(executed);
		}

		[Test]
		public void AsyncTimer_Does_Not_Yield()
		{
			//Arrange
			var num = 1;

			var timer = new AsyncTimer(async () =>
			{
				num++;
				await Task.Delay(2000);
			}, TimeSpan.FromSeconds(1));
			
			//Act
			timer.Start();
			Thread.Sleep(1200);
			timer.Stop();

			//Assert
			Assert.Equal(3, num);
		}

		[Test]
		public void AsyncTimer_Does_Yield_When_True()
		{
			//Arrange
			var num = 1;
			var timer = new AsyncTimer(async () =>
			{
				num++;
				await Task.Delay(3000);
			}, TimeSpan.FromSeconds(2), true);
			
			//Act
			timer.Start();
			Thread.Sleep(2500);
			timer.Stop();
			
			//Assert
			Assert.Equal(2, num);
		}

		[Test]
		public void AsyncTimer_Does_Not_StartTwice()
		{
			//Arange
			using var timer = new AsyncTimer(() => Task.FromResult(0), TimeSpan.FromSeconds(1));
			
			//Act
			timer.Start();
			Thread.Sleep(200);
			
			//Assert
			Assert.Throws<InvalidOperationException>(() => timer.Start());
		}

		[Test]
		public void AsyncTimer_Does_Not_Stop_If_NotStarted()
		{
			//Arrange
			var timer = new AsyncTimer(() => Task.CompletedTask, TimeSpan.Zero);
			
			//Act
			
			//Assert
			Assert.Throws<InvalidOperationException>(() => timer.Stop());
		}
	}
}