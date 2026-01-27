using MoonTools.ECS;
using MoonWorks.Audio;
using Tactician.Content;
using Tactician.Components;
using Tactician.Data;

namespace Tactician.Messages;

public readonly record struct PlayStaticSoundMessage(
	StaticSoundID StaticSoundID,
	SoundCategory Category = SoundCategory.Generic,
	float         Volume   = 1,
	float         Pitch    = 0,
	float         Pan      = 0
)
{
	public AudioBuffer Sound => StaticAudio.Lookup(StaticSoundID);
}

public readonly record struct SetAnimationMessage(
	Entity          Entity,
	SpriteAnimation Animation,
	bool            ForceUpdate = false
);

public readonly record struct PlaySongMessage;

public readonly record struct EndGame;

// Chess messages
public readonly record struct ExecuteMoveMessage(ChessMove Move);
public readonly record struct CheckMessage(Player PlayerInCheck);
public readonly record struct CheckmateMessage(Player Winner);
public readonly record struct StalemateMessage();