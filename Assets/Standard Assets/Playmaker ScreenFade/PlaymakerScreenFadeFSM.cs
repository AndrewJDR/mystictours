using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace HutongGames.PlayMaker.Actions {
	[ActionCategory("ScreenFade")]
	public abstract class Fade : FsmStateAction {
		public ScreenFader.Direction direction;

		[RequiredField]
		[Tooltip("Color to fade from. E.g., Fade up from black.")]
		public FsmColor color;
		
		[RequiredField]
		[HasFloatSlider(0,10)]
		[Tooltip("Fade in time in seconds.")]
		public FsmFloat time;
		
		[Tooltip("Event to send when finished.")]
		public FsmEvent finishEvent;
		
		[Tooltip("Ignore TimeScale. Useful if the game is paused.")]
		public bool realTime;
		public Texture texture = null;

		public override void Reset() {
			color = Color.black;
			time = 1.0f;
			finishEvent = null;
			texture = null;
		}
		
		protected ScreenFader screenFader;
		protected Texture tex;

		public override void OnEnter() {
			tex = texture;
			if (tex == null)
				tex = ActionHelpers.WhiteTexture;
			screenFader = ScreenFader.GetInstance();
			screenFader.Stop();
			screenFader.time = time.Value;
			screenFader.direction = direction;
			screenFader.fader = Fader;
			screenFader.onComplete = Finish;
			screenFader.Run();
		}

		protected abstract void Fader(float f);
	
		protected float GetLinearT(float fadeBalance, int i, int from) {
			return fadeBalance / ((float)i / (float)from);
		}

		protected float GetNonLinearT(float fadeBalance, int i, int from) {
			return fadeBalance * from - fadeBalance * i;
		}
	}

	[ActionCategory("ScreenFade")]
	[Tooltip("Set to fade state")]
	public class ScreenFadeSet : FsmStateAction {
		public ScreenFader.Direction direction;
		[RequiredField]
		[Tooltip("Color to fade from. E.g., Fade up from black.")]
		public FsmColor color;

		public override void OnEnter() {
			ScreenFader screenFader = ScreenFader.GetInstance();
			screenFader.Stop();
			screenFader.fader = (f) => {
				GUI.color = color.Value;
				GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), ActionHelpers.WhiteTexture);
			};
			screenFader.SetToState(direction);
			Finish();
		}
	}

	[Tooltip("Default Screen Fade effect")]
	public class ScreenFadeDefault : Fade {
		protected override void Fader(float f) {
			GUI.color = Color.Lerp(Color.clear, color.Value, f);
			GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), tex);
		}
	}

	[Tooltip("Scale Screen Fade effect")]
	public class ScreenFadeScale : Fade {
		public FsmVector2 screenPosition;
		public FsmFloat startScale;
		public FsmFloat endScale;
		public bool fadeColor = false, withBorders = true;


		public override void Reset() {
			base.Reset();
			startScale = new FsmFloat();
			endScale = new FsmFloat();
			startScale.Value = 0;
			endScale.Value = 2;
			withBorders = true;
			fadeColor = false;
		}

		protected override void Fader(float f) {
			Vector2 pos = screenPosition.Value;
			pos.x+= Screen.width / 2;
			pos.y+= Screen.height / 2;
			float size = ((endScale.Value - startScale.Value) * (1 - f) + startScale.Value) * Screen.width;
			GUI.color = fadeColor ? Color.Lerp(Color.clear, color.Value, f * 2) : color.Value;
			Rect rect = new Rect(pos.x - size / 2, pos.y - size / 2, size, size);
			GUI.DrawTexture(rect, tex);
			if (withBorders) {
				GUI.DrawTexture(new Rect(0, 0, Screen.width, rect.y), ActionHelpers.WhiteTexture);
				GUI.DrawTexture(new Rect(0, rect.yMax, Screen.width, Screen.height - rect.yMax), ActionHelpers.WhiteTexture);
				GUI.DrawTexture(new Rect(0, rect.y, rect.x, rect.yMax - rect.y), ActionHelpers.WhiteTexture);
				GUI.DrawTexture(new Rect(rect.xMax, rect.y, Screen.width - rect.xMax, rect.yMax - rect.y), ActionHelpers.WhiteTexture);
			}
		}
	}

	[Tooltip("Squared Screen Fade effect")]
	public class ScreenFadeSquared : Fade {
		struct AnimRect {
			private Rect rect;
			public float fromScale;
			public float toScale;
			
			public AnimRect(Rect rect, float fromScale, float toScale) {
				this.rect = rect;
				this.fromScale = fromScale;
				this.toScale = toScale;
			}
			
			public Rect GetRect(float time) {
				if (time >= 1)
					return rect;
				else if (time < 0)
					return new Rect(rect.xMin + rect.width * time / 2, rect.yMin + rect.height * time / 2, 0, 0);
				else
					return new Rect(
						rect.x + rect.width / 2 * (0.5F - time / 2),
						rect.y + rect.height / 2 * (0.5F - time / 2),
						rect.width * time,
						rect.height * time);
			}
		}

		public enum Direction { NONE, HORIZONTAL_LEFT, HORIZONTAL_RIGHT, VERTICAL_UP, VERTICAL_DOWN, DIAGONAL_LEFT_DOWN, DIAGONAL_LEFT_UP, DIAGONAL_RIGHT_UP, DIAGONAL_RIGHT_DOWN }

		public Direction moveDirection = Direction.DIAGONAL_LEFT_DOWN;
		[Range(2, 50)]
		public int columns = 10;

		int rows;
		AnimRect[,] squares = null;

		public override void OnEnter() {
			int w = Screen.width + columns;
			int h = Screen.height + columns;
			rows = h / (w / columns) + 2;
			squares = new AnimRect[columns, rows];
			for (int c = 0; c < columns; c++) {
				for (int r = 0; r < rows; r++) {
					squares[c, r] = new AnimRect(
						new Rect(
						w / columns * c,
						h / rows * r,
						w / columns,
						h / rows)
						, 0.1f, 1f);
				}
			}
			base.OnEnter();
		}

		public override void OnExit() {
			base.OnExit();
			squares = null;
			tex = null;
		}

		protected override void Fader(float f) {
			GUI.color = color.Value;
			for (int i = 0; i < columns; i++) {
				for (int y = 0; y < rows; y++) {
					switch (moveDirection) {
					case Direction.DIAGONAL_LEFT_DOWN:
						GUI.DrawTexture(squares[i, y].GetRect(f / ((float)(i + y) / (float)(columns + rows))), tex);
						break;
					case Direction.DIAGONAL_LEFT_UP:
						GUI.DrawTexture(squares[columns - i-1, rows - y - 1].GetRect(f / ((float)(i + y) / (float)(columns + rows))), tex);
						break;
					case Direction.DIAGONAL_RIGHT_DOWN:
						GUI.DrawTexture(squares[columns - i - 1, y].GetRect(f / ((float)(i + y) / (float)(columns + rows))), tex);
						break;
					case Direction.DIAGONAL_RIGHT_UP:
						GUI.DrawTexture(squares[i, rows - y - 1].GetRect(f / ((float)(i + y) / (float)(columns + rows))), tex);
						break;
						
					case Direction.VERTICAL_DOWN:
						GUI.DrawTexture(squares[i, y].GetRect(GetLinearT(f, y, rows)), tex);
						break;
					case Direction.VERTICAL_UP:
						GUI.DrawTexture(squares[i, rows - y - 1].GetRect(GetLinearT(f, y, rows)), tex);
						break;
						
					case Direction.HORIZONTAL_RIGHT:
						GUI.DrawTexture(squares[i, y].GetRect(GetLinearT(f, i, columns)), tex);
						break;
					case Direction.HORIZONTAL_LEFT:
						GUI.DrawTexture(squares[columns - i-1, rows-y-1].GetRect(GetLinearT(f, i, columns)), tex);
						break;

					case Direction.NONE:
						GUI.DrawTexture(squares[i, y].GetRect(f), tex);
						break;
					}
				}
			}
		}
	}

	[Tooltip("Stripe Screen Fade effect")]
	public class ScreenFadeStripe : Fade {
		struct AnimRect {
			private Rect rect;
			public float fromScale;
			public float toScale;

			public AnimRect(Rect rect, float fromScale, float toScale) {
				this.rect = rect;
				this.fromScale = fromScale;
				this.toScale = toScale;
			}
			
			public Rect GetRect(float time) {
				if (time >= 1)
					return rect;
				else if (time < 0)
					return new Rect(rect.xMin + rect.width * time / 2, rect.yMin + rect.height * time / 2, 0, 0);
				else
					return new Rect(rect.xMin + (rect.width - rect.width * time) / 2, rect.yMin + (rect.height - rect.height * time) / 2 * time, rect.width * time, rect.height * time);
			}
		}

		public enum Direction { HORIZONTAL_LEFT, HORIZONTAL_RIGHT, HORIZONTAL_IN, HORIZONTAL_OUT }

		[Range(2, 50)]
		public int numberOfStripes = 10;
		public Direction moveDirection = Direction.HORIZONTAL_LEFT;

		AnimRect[] rcs = null;

		public override void OnEnter() {
			rcs = new AnimRect[numberOfStripes];
			int a = Screen.width / rcs.Length * 3;
			for (int i = 0; i < rcs.Length; i++) {
				rcs[i] = new AnimRect(
					new Rect((Screen.width + a) / rcs.Length * i - 5, -5, (Screen.width + a) / rcs.Length, Screen.height + 10), 
					0.1f, 
					1f
				);
			}
			base.OnEnter();
		}
		
		public override void OnExit() {
			base.OnExit();

			rcs = null;
		}

		protected override void Fader(float f) {
			GUI.color = color.Value;
			for (int i = 0; i < rcs.Length; i++) {
				switch (moveDirection) {
				case Direction.HORIZONTAL_LEFT:
					GUI.DrawTexture(rcs[i].GetRect(GetLinearT(f, i, rcs.Length)), tex);
					break;
				case Direction.HORIZONTAL_RIGHT:
					GUI.DrawTexture(rcs[rcs.Length - i - 1].GetRect(GetLinearT(f, i, rcs.Length)), tex);
					break;
				case Direction.HORIZONTAL_IN:
					GUI.DrawTexture(rcs[rcs.Length - i - 1].GetRect(GetLinearT(f, i * 2, rcs.Length)), tex);
					GUI.DrawTexture(rcs[i].GetRect(GetLinearT(f, i * 2, rcs.Length)), tex);
					break;
				case Direction.HORIZONTAL_OUT:
					if (i < rcs.Length / 2) {
						GUI.DrawTexture(rcs[rcs.Length / 2 - i - 1].GetRect(GetLinearT(f, i * 2, rcs.Length)), tex);
						GUI.DrawTexture(rcs[rcs.Length / 2 + i].GetRect(GetLinearT(f, i * 2, rcs.Length)), tex);
					}
					break;
				default:
					break;
				}
				if ((moveDirection == Direction.HORIZONTAL_IN || moveDirection == Direction.HORIZONTAL_OUT) && i > (rcs.Length / 2) + 1)
					break;
			}
		}
	}
}
