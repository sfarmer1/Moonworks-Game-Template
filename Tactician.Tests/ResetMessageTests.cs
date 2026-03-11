using MoonTools.ECS;
using Tactician.Messages;

namespace Tactician.Tests;

/// <summary>
/// Simple unit tests for the reset message functionality that don't require content loading.
/// </summary>
public class ResetMessageTests
{
	[Fact]
	public void ResetMessage_CanBeSent_AndIsDetected()
	{
		// Arrange
		var world = new World();

		// Act
		world.Send(new ResetGameMessage());

		// Assert
		Assert.True(world.SomeMessage<ResetGameMessage>());
	}

	[Fact]
	public void ResetMessage_IsClearedAfterFinishUpdate()
	{
		// Arrange
		var world = new World();
		world.Send(new ResetGameMessage());
		Assert.True(world.SomeMessage<ResetGameMessage>());

		// Act
		world.FinishUpdate();

		// Assert
		Assert.False(world.SomeMessage<ResetGameMessage>());
	}

	[Fact]
	public void MultipleResetMessages_CanBeSent()
	{
		// Arrange
		var world = new World();

		// Act
		world.Send(new ResetGameMessage());
		world.Send(new ResetGameMessage());
		world.Send(new ResetGameMessage());

		// Assert
		Assert.True(world.SomeMessage<ResetGameMessage>());
		world.FinishUpdate();
		Assert.False(world.SomeMessage<ResetGameMessage>());
	}
}
