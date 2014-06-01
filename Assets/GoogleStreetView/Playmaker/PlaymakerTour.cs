using UnityEngine;
using System.Collections;

namespace HutongGames.PlayMaker.Actions {
	[ActionCategory("Tour")]
	public class LocationInit : FsmStateAction {
		public override void OnEnter() {
			Tour tour = GameObject.FindObjectOfType<Tour>();
			tour.Init();
			Finish();
		}
	}

	[ActionCategory("Tour")]
	public class NextLocation : FsmStateAction {
		public override void OnEnter() {
			Tour tour = GameObject.FindObjectOfType<Tour>();
			tour.Next();
			Finish();
		}
	}

	[ActionCategory("Tour")]
	public class PrevLocation : FsmStateAction {
		public override void OnEnter() {
			Tour tour = GameObject.FindObjectOfType<Tour>();
			tour.Prev();
			Finish();
		}
	}

	[ActionCategory("Tour")]
	public class PlayAudio : FsmStateAction {
		public override void OnEnter() {
			Tour tour = GameObject.FindObjectOfType<Tour>();
			tour.PlayAudio(Finish);
		}
	}

	[ActionCategory("Tour")]
	public class PlayAmbientAudio : FsmStateAction {
		public override void OnEnter() {
			Tour tour = GameObject.FindObjectOfType<Tour>();
			tour.PlayAmbientAudio();
			Finish();
		}
	}
	
	[ActionCategory("Tour")]
	public class StopAmbientAudio : FsmStateAction {
		public override void OnEnter() {
			Tour tour = GameObject.FindObjectOfType<Tour>();
			tour.StopAmbientAudio();
			Finish();
		}
	}
	
}
