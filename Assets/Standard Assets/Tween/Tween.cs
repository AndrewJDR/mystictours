using UnityEngine;
using System.Collections;

public class Tween : MonoBehaviour {
	public enum LoopType {
		none,
		loop,
		pingpong
	}

	public static Tween instance;
	static Base[] tweens = new Base[100];
	static int count = 0;
	static float deltaTime;

	void Update() {
		unchecked {
			if (count > 0) {
				deltaTime = Time.deltaTime;
				int d = 0;
				for (int i=0; i<count; i++) {
					Base b = tweens[i];
					if (!b.stop) {
						b.Update();
						if (i != d)
							tweens[d] = b;
						d++;
					}
				}
				if (count != d) {
					for (int i=d; i<count; i++)
						tweens[i] = null;
					count = d;
				}
			}
		}
	}

	public static void Stop(object target) {
		Transform t = null;
		if (target is Component) t = ((Component)target).transform;
		else if (target is GameObject) t = ((GameObject)target).transform;
		if (t != null)
			for (int i=0; i<count; i++)
				if (tweens[i].transform == t)
					tweens[i].Stop();
	}

	public static void Stop(object target, string tag) {
		Transform t = null;
		if (target is Component) t = ((Component)target).transform;
		else if (target is GameObject) t = ((GameObject)target).transform;
		if (t != null)
			for (int i=0; i<count; i++)
				if (tweens[i].transform == t && (tag == null || (tweens[i].tags != null && System.Array.IndexOf(tweens[i].tags, tag) >= 0)))
					tweens[i].Stop();
	}

	public static void Stop(params string[] tags) {
		foreach (string tag in tags)
			for (int i=0; i<count; i++)
				if (tweens[i].tags != null && !tweens[i].stop && System.Array.IndexOf(tweens[i].tags, tag) >= 0) {
					tweens[i].Stop();
					break;
				}
	}

	/*
	 * 
	 */
	public static TweenValue Value(float time=1, float delay=0) {
		return new TweenValue() {time=time, delay=delay};
	}
	
	public static TweenMove Move(object target=null, float time=1, float delay=0) {
		return new TweenMove() {target=target, time=time, delay=delay};
	}
	
	public static TweenRotate Rotate(object target=null, float time=1, float delay=0) {
		return new TweenRotate() {target=target, time=time, delay=delay};
	}
	
	public static TweenScale Scale(object target=null, float time=1, float delay=0) {
		return new TweenScale() {target=target, time=time, delay=delay};
	}
	
	public static Base Delay(float time, System.Action action) {
		return new Base() {time = time}.OnComplete(action);
	}
	
	/*
	 * 
	 */
	public class Base {
		public bool stop = true;
		public float time = .5f, delay = 0;
		protected float runningTime, percentage = 0, easevalue = 0;
		public LoopType looptype = LoopType.none;
		protected System.Func<float, float> ease = Tween.Ease.easeInOutQuad;
		
		public object target;
		public Transform transform;
		protected bool reverse = false;
		
		public string[] tags;
		protected event System.Action onStart;
		protected Base[] onStartTweens;
		protected int onStartTweensCount = 0;
		protected System.Action onComplete;
		protected Base[] onCompleteTweens;
		protected int onCompleteTweensCount = 0;
		protected System.Action onFinal;
		protected System.Action onUpdate;
		protected System.Action<float> onUpdateFloat;
		
		protected virtual void Init() {}
		protected virtual void Apply() {}
		protected virtual void Cancel() {}
		
		protected virtual void Updateable() {
			if (count == tweens.Length)
				System.Array.Resize<Base>(ref tweens, tweens.Length * 2);
			tweens[count++] = this;
		}
		
		public virtual void Update() {
			unchecked {
				if (runningTime < 0) {
					if ((runningTime+= deltaTime) >= 0) {
						runningTime = 0;
						Started();
					}
					return;
				}
				if ((percentage >= 1 && !reverse) || (percentage <= 0 && reverse)) {
					// completed
					percentage = Mathf.Clamp01(percentage);
					easevalue = ease(percentage);
					Apply();
					if (onUpdate != null)
						onUpdate();
					if (onUpdateFloat != null)
						onUpdateFloat(easevalue);
					runningTime = 0;
					if (looptype == Tween.LoopType.loop)
						percentage = 0;
					else if (looptype == Tween.LoopType.pingpong)
						reverse = !reverse;
					else
						Stop();
					if (onComplete != null)
						onComplete();
					if (onCompleteTweens != null)
						Callback(onCompleteTweens, onCompleteTweensCount);
				} else {
					easevalue = ease(percentage);
					Apply();
					if (onUpdate != null)
						onUpdate();
					if (onUpdateFloat != null)
						onUpdateFloat(easevalue);
					runningTime+= deltaTime;
					percentage = reverse ? 1 - runningTime / time : runningTime / time;
				}
			}
		}
		
		protected void Started() {
			percentage = 0;
			reverse = false;
			Init();
			if (transform != null && this is TweenVector3)
				for (int i=0; i<count; i++)
					if (!tweens[i].stop && tweens[i] != this && tweens[i].transform == transform && tweens[i].GetType() == this.GetType() && ((TweenVector3)tweens[i]).CheckStop((TweenVector3)this))
						tweens[i].Stop();
			if (onStart != null)
				onStart();
			if (onStartTweens != null)
				Callback(onStartTweens, onStartTweensCount);
		}
		
		public void Stop() {
			Cancel();
			if (onFinal != null)
				onFinal();
			stop = true;
		}
		
		public Base Start() {
			if (instance == null) {
				GameObject go = GameObject.Find("Tween");
				if (go == null)
					go = new GameObject("Tween");
				instance = go.GetComponent<Tween>();
				if (instance == null)
					instance = go.AddComponent<Tween>();
			}
			if (transform == null && target != null) {
				if (target is Component) transform = ((Component)target).transform;
				else if (target is GameObject) transform = ((GameObject)target).transform;
			}
			runningTime = -delay;
			if (runningTime >= 0)
				Started();
			stop = false;
			Updateable();
			return this;
		}
		
		protected void Callback(Base[] array, int count) {
			for (int i=0; i<count; i++) {
				Base b = array[i];
				if (b.target == null)
					b.target = target;
				b.Start();
			}
		}
		
		protected void Callback(System.Action[] array, int count) {
			for (int i=0; i<count; i++)
				array[i]();
		}
		
		protected void Callback<T>(System.Action<T>[] array, int count, T obj) {
			for (int i=0; i<count; i++)
				array[i](obj);
		}
		
		protected static void AddToArray<T>(ref T[] array, ref int count, T obj) {
			if (array == null) {
				array = new T[1] {obj};
				count = 1;
			} else {
				if (count == array.Length)
					System.Array.Resize<T>(ref array, array.Length * 2);
				array[count++] = obj;
			}
		}
		
		/*
		 */
		public Base Once {
			get {
				this.looptype = LoopType.none;
				return this;
			}
		}
		
		public Base Loop {
			get {
				this.looptype = LoopType.loop;
				return this;
			}
		}
		
		public Base PingPong {
			get {
				this.looptype = LoopType.pingpong;
				return this;
			}
		}
		
		public Base Ease(System.Func<float, float> fn) {
			this.ease = fn;
			return this;
		}
		
		public Base Ease(AnimationCurve easeCurve) {
			this.ease = (f) => easeCurve.Evaluate(f);
			return this;
		}
		
		public Base Time(float time) {
			this.time = time;
			return this;
		}
		
		public Base Tags(params string[] tags) {
			this.tags = tags;
			return this;
		}
		
		public Base Target(object t) {
			this.target = t;
			if (target is Component) transform = ((Component)target).transform;
			else if (target is GameObject) transform = ((GameObject)target).transform;
			return this;
		}
		
		public Base Start(object target) {
			this.target = target;
			return Start();
		}
		
		public Base Start(object target, float delay) {
			this.target = target;
			this.delay = delay;
			return Start();
		}
		
		public Base Start(float delay) {
			this.delay = delay;
			return Start();
		}
		
		public Base OnStart(Base tween) {
			AddToArray<Base>(ref onStartTweens, ref onStartTweensCount, tween);
			return this;
		}
		
		public Base OnStart(System.Action callback) {
			if (callback != null)
				onStart+= callback;
			return this;
		}
		
		public Base OnComplete(Base tween) {
			AddToArray<Base>(ref onCompleteTweens, ref onCompleteTweensCount, tween);
			return this;
		}
		
		public Base OnComplete(System.Action callback) {
			if (callback != null)
				onComplete+= callback;
			return this;
		}
		
		public Base OnFinal(System.Action callback) {
			if (callback != null)
				onFinal+= callback;
			return this;
		}
		
		public Base OnUpdate(System.Action callback) {
			if (callback != null)
				onUpdate+= callback;
			return this;
		}
		
		public Base OnUpdate(System.Action<float> callback) {
			if (callback != null)
				onUpdateFloat+= callback;
			return this;
		}
	}
	
	
	public class TweenValue : Base {
		protected float start = 0, end = 1;
		
		public TweenValue() {
			this.ease = Tween.Ease.linear;
		}
		
		public TweenValue From(float v) {
			start = v;
			return this;
		}
		
		public TweenValue To(float v) {
			end = v;
			return this;
		}
		
		protected override void Apply() {
			unchecked {
				easevalue = start + (end - start) * easevalue;
			}
		}
	}
	
	public abstract class TweenVector3 : Base {
		protected Vector3 start, end, amount;
		protected Transform startTransform, endTransform;
		protected bool startx = false, starty = false, startz = false, endx = false, endy = false, endz = false, useAmount = false;
		protected bool isLocal = true;
		protected Vector3 valueVector3;
		protected event System.Action<Vector3> onUpdateVector3;

		public bool CheckStop(TweenVector3 v) {
			return (((startx || endx) && (v.startx || v.endx)) ||
			    	((starty || endy) && (v.starty || v.endy)) ||
			        ((startz || endz) && (v.startz || v.endz)));
		}

		public TweenVector3 FromX(float x) {
			start.x = x;
			startx = true;
			return this;
		}
		
		public TweenVector3 FromY(float y) {
			start.y = y;
			starty = true;
			return this;
		}
		
		public TweenVector3 FromZ(float z) {
			start.z = z;
			startz = true;
			return this;
		}
		
		public TweenVector3 From(float x, float y) {
			start.x = x;
			startx = true;
			start.y = y;
			starty = true;
			return this;
		}
		
		public TweenVector3 From(float x, float y, float z) {
			start.x = x;
			startx = true;
			start.y = y;
			starty = true;
			start.z = z;
			startz = true;
			return this;
		}
		
		public TweenVector3 From(Vector3 start) {
			this.start = start;
			startx = true;
			starty = true;
			startz = true;
			return this;
		}
		
		public TweenVector3 From(Component c) {
			this.startTransform = c.transform;
			return this;
		}
		
		public TweenVector3 ToX(float x) {
			end.x = x;
			endx = true;
			return this;
		}
		
		public TweenVector3 ToY(float y) {
			end.y = y;
			endy = true;
			return this;
		}
		
		public TweenVector3 ToZ(float z) {
			end.z = z;
			endz = true;
			return this;
		}
		
		public TweenVector3 To(float x, float y) {
			end.x = x;
			endx = true;
			end.y = y;
			endy = true;
			return this;
		}
		
		public TweenVector3 To(float x, float y, float z) {
			end.x = x;
			endx = true;
			end.y = y;
			endy = true;
			end.z = z;
			endz = true;
			return this;
		}
		
		public TweenVector3 To(Vector3 end) {
			this.end = end;
			endx = true;
			endy = true;
			endz = true;
			return this;
		}
		
		public TweenVector3 To(Component c) {
			this.endTransform = c.transform;
			return this;
		}
		
		public TweenVector3 Local {
			get {
				isLocal = true;
				return this;
			}
		}
		
		public TweenVector3 Global {
			get {
				isLocal = false;
				return this;
			}
		}
		
		public Base OnUpdate(System.Action<Vector3> callback) {
			onUpdateVector3+= callback;
			return this;
		}
		
		protected void Init(Vector3 pos) {
			if (!startx) start.x = pos.x;
			if (!starty) start.y = pos.y;
			if (!startz) start.z = pos.z;
			if (useAmount) {
				end = start + amount;
			} else {
				if (!endx) end.x = pos.x;
				if (!endy) end.y = pos.y;
				if (!endz) end.z = pos.z;
			}
			valueVector3 = start;
		}
		
		protected void Apply(Vector3 pos) {
			unchecked {
				valueVector3 = start + (end - start) * easevalue;
			}
			if (!startx && !endx) valueVector3.x = pos.x;
			if (!starty && !endy) valueVector3.y = pos.y;
			if (!startz && !endz) valueVector3.z = pos.z;
			if (onUpdateVector3 != null)
				onUpdateVector3(valueVector3);
		}
	}
	
	public class TweenMove : TweenVector3 {
		protected override void Init() {
			if (!transform) {
				Debug.LogError("target is null on Tween.Move.Init");
				Stop();
				return;
			}
			base.Init();
			base.Init(isLocal ? transform.localPosition : transform.position);
			if (startTransform != null) start = isLocal ? startTransform.localPosition : startTransform.position;
			if (endTransform != null) end = isLocal ? endTransform.localPosition : endTransform.position;
		}
		
		protected override void Apply() {
			if (!transform) {
				Debug.LogError("target is null on Tween.Move");
				Stop();
				return;
			}
			base.Apply(isLocal ? transform.localPosition : transform.position);
			if (isLocal) transform.localPosition = valueVector3;
			else transform.position = valueVector3;
		}
	}
	
	public class TweenRotate : TweenVector3 {
		Vector3 aroundPoint, aroundAxis;
		float aroundAngle;
		bool rotateRound = false;
		
		public TweenRotate Around(Vector3 point, Vector3 axis, float angle) {
			aroundPoint = point;
			aroundAxis = axis;
			aroundAngle = angle;
			rotateRound = true;
			return this;
		}
		
		protected override void Init() {
			if (!transform) {
				Debug.LogError("target is null on Tween.Rotate.Init");
				Stop();
				return;
			}
			base.Init();
			base.Init(isLocal ? transform.localEulerAngles : transform.eulerAngles);
			if (startTransform != null) start = isLocal ? startTransform.localEulerAngles : startTransform.eulerAngles;
			if (endTransform != null) end = isLocal ? endTransform.localEulerAngles : endTransform.eulerAngles;
			unchecked {
				Vector3 st = start;
				Vector3 en = end;
				float sx0 = Mathf.Abs(st.x - en.x), sx1 = Mathf.Abs((st.x - 360) - en.x), sx2 = Mathf.Abs(st.x - (en.x - 360));
				if (sx1 < sx0 && sx1 < sx2) st.x = st.x - 360;
				else if (sx2 < sx0 && sx2 < sx1) en.x = en.x - 360;
				
				float sy0 = Mathf.Abs(st.y - en.y), sy1 = Mathf.Abs((st.y - 360) - en.y), sy2 = Mathf.Abs(st.y - (en.y - 360));
				if (sy1 < sy0 && sy1 < sy2) st.y = st.y - 360;
				else if (sy2 < sy0 && sy2 < sy1) en.y = en.y - 360;
				
				float sz0 = Mathf.Abs(st.z - en.z), sz1 = Mathf.Abs((st.z - 360) - en.z), sz2 = Mathf.Abs(st.z - (en.z - 360));
				if (sz1 < sz0 && sz1 < sz2) st.z = st.z - 360;
				else if (sz2 < sz0 && sz2 < sz1) en.z = en.z - 360;
				start = st;
				end = en;
			}
		}
		
		protected override void Apply() {
			if (!transform) {
				Debug.LogError("target is null on Tween.Rotate");
				Stop();
				return;
			}
			if (rotateRound) {
				transform.RotateAround(aroundPoint, aroundAxis, aroundAngle * deltaTime / time);
			} else {
				base.Apply(isLocal ? transform.localEulerAngles : transform.eulerAngles);
				if (isLocal) transform.localEulerAngles = valueVector3;
				else transform.eulerAngles = valueVector3;
			}
		}
	}	

	public class TweenScale : TweenVector3 {
		protected override void Init() {
			if (!transform) {
				Debug.LogError("target is null on Tween.Scale.Init");
				Stop();
				return;
			}
			base.Init();
			base.Init(transform.localScale);
			if (startTransform != null) start = startTransform.localScale;
			if (endTransform != null) end = endTransform.localScale;
		}

		protected override void Apply() {
			if (!transform) {
				Debug.LogError("target is null on Tween.Scale");
				Stop();
				return;
			}
			base.Apply(transform.localScale);
			transform.localScale = valueVector3;
		}
	}

	#region Ease Functions
	public static class Ease {
		public static float linear(float value) {
			return value;
		}

		public static float clerp(float value) {
			unchecked {
				float min = 0.0f;
				float max = 360.0f;
				float half = Mathf.Abs((max - min) * 0.5f);
				float retval = 0.0f;
				float diff = 0.0f;
				if (1 < -half) {
					diff = (max + 1) * value;
					retval = diff;
				} else if (1 > half) {
					diff = -(max - 1) * value;
					retval = diff;
				} else retval = value;
				return retval;
			}
		}
		
		public static float spring(float value) {
			unchecked {
				value = Mathf.Clamp01(value);
				value = (Mathf.Sin(value * Mathf.PI * (0.2f + 2.5f * value * value * value)) * Mathf.Pow(1f - value, 2.2f) + value) * (1f + (1.2f * (1f - value)));
				return value;
			}
		}
		
		public static float easeInQuad(float value) {
			return value * value;
		}
		
		public static float easeOutQuad(float value) {
			return -value * (value - 2);
		}
		
		public static float easeInOutQuad(float value) {
			unchecked {
				value /= .5f;
				if (value < 1) return 0.5f * value * value;
				value--;
				return -0.5f * (value * (value - 2) - 1);
			}
		}
		
		public static float easeInCubic(float value) {
			return value * value * value;
		}
		
		public static float easeOutCubic(float value) {
			value--;
			return (value * value * value + 1);
		}
		
		public static float easeInOutCubic(float value) {
			unchecked {
				value /= .5f;
				if (value < 1) return 0.5f * value * value * value;
				value -= 2;
				return 0.5f * (value * value * value + 2);
			}
		}
		
		public static float easeInQuart(float value) {
			return value * value * value * value;
		}
		
		public static float easeOutQuart(float value) {
			value--;
			return -(value * value * value * value - 1);
		}
		
		public static float easeInOutQuart(float value) {
			unchecked {
				value /= .5f;
				if (value < 1) return 0.5f * value * value * value * value;
				value -= 2;
				return -0.5f * (value * value * value * value - 2);
			}
		}
		
		public static float easeInQuint(float value){
			return value * value * value * value * value;
		}
		
		public static float easeOutQuint(float value){
			value--;
			return (value * value * value * value * value + 1);
		}
		
		public static float easeInOutQuint(float value){
			unchecked {
				value /= .5f;
				if (value < 1) return 0.5f * value * value * value * value * value;
				value -= 2;
				return 0.5f * (value * value * value * value * value + 2);
			}
		}
		
		public static float easeInSine(float value){
			return -Mathf.Cos(value * (Mathf.PI * 0.5f)) + 1;
		}
		
		public static float easeOutSine(float value){
			return Mathf.Sin(value * (Mathf.PI * 0.5f));
		}
		
		public static float easeInOutSine(float value){
			return -0.5f * (Mathf.Cos(Mathf.PI * value) - 1);
		}
		
		public static float easeInExpo(float value){
			return Mathf.Pow(2, 10 * (value - 1));
		}
		
		public static float easeOutExpo(float value){
			return (-Mathf.Pow(2, -10 * value ) + 1);
		}
		
		public static float easeInOutExpo(float value){
			value /= .5f;
			if (value < 1) return 0.5f * Mathf.Pow(2, 10 * (value - 1));
			value--;
			return 0.5f * (-Mathf.Pow(2, -10 * value) + 2);
		}
		
		public static float easeInCirc(float value){
			return -(Mathf.Sqrt(1 - value * value) - 1);
		}
		
		public static float easeOutCirc(float value){
			value--;
			return Mathf.Sqrt(1 - value * value);
		}
		
		public static float easeInOutCirc(float value){
			unchecked {
				value /= .5f;
				if (value < 1) return -0.5f * (Mathf.Sqrt(1 - value * value) - 1);
				value -= 2;
				return 0.5f * (Mathf.Sqrt(1 - value * value) + 1);
			}
		}
		
		/* GFX47 MOD START */
		public static float easeInBounce(float value){
			float d = 1f;
			return easeOutBounce(d-value);
		}
		/* GFX47 MOD END */
		
		/* GFX47 MOD START */
		//private float bounce(float value){
		public static float easeOutBounce(float value){
			value /= 1f;
			if (value < (1 / 2.75f)){
				return (7.5625f * value * value);
			}else if (value < (2 / 2.75f)){
				value -= (1.5f / 2.75f);
				return (7.5625f * (value) * value + .75f);
			}else if (value < (2.5 / 2.75)){
				value -= (2.25f / 2.75f);
				return (7.5625f * (value) * value + .9375f);
			}else{
				value -= (2.625f / 2.75f);
				return (7.5625f * (value) * value + .984375f);
			}
		}
		/* GFX47 MOD END */
		
		/* GFX47 MOD START */
		public static float easeInOutBounce(float value){
			float d = 1f;
			if (value < d* 0.5f) return easeInBounce(value*2) * 0.5f;
			else return easeOutBounce(value*2-d) * 0.5f + 0.5f;
		}
		/* GFX47 MOD END */
		
		public static float easeInBack(float value){
			value /= 1;
			float s = 1.70158f;
			return value * value * ((s + 1) * value - s);
		}
		
		public static float easeOutBack(float value){
			float s = 1.70158f;
			value = (value) - 1;
			return ((value) * value * ((s + 1) * value + s) + 1);
		}
		
		public static float easeInOutBack(float value){
			float s = 1.70158f;
			value /= .5f;
			if ((value) < 1){
				s *= (1.525f);
				return 0.5f * (value * value * (((s) + 1) * value - s));
			}
			value -= 2;
			s *= (1.525f);
			return 0.5f * ((value) * value * (((s) + 1) * value + s) + 2);
		}
		
		/*static float punch(float amplitude, float value){
			float s = 9;
			if (value == 0){
				return 0;
			}
			else if (value == 1){
				return 0;
			}
			float period = 1 * 0.3f;
			s = period / (2 * Mathf.PI) * Mathf.Asin(0);
			return (amplitude * Mathf.Pow(2, -10 * value) * Mathf.Sin((value * 1 - s) * (2 * Mathf.PI) / period));
		}*/
		
		/* GFX47 MOD START */
		public static float easeInElastic(float value){
			float d = 1f;
			float p = d * .3f;
			float s = 0;
			float a = 0;
			
			if (value == 0) return 0;
			
			if ((value /= d) == 1) return 1;
			
			if (a == 0f || a < 1){
				a = 1;
				s = p / 4;
			}else{
				s = p / (2 * Mathf.PI) * Mathf.Asin(1 / a);
			}
			
			return -(a * Mathf.Pow(2, 10 * (value-=1)) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p));
		}		
		/* GFX47 MOD END */
		
		/* GFX47 MOD START */
		//private float elastic(float value){
		public static float easeOutElastic(float value){
			/* GFX47 MOD END */
			//Thank you to rafael.marteleto for fixing this as a port over from Pedro's UnityTween
			float d = 1f;
			float p = d * .3f;
			float s = 0;
			float a = 0;
			
			if (value == 0) return 0;
			
			if ((value /= d) == 1) return 1;
			
			if (a == 0f || a < 1){
				a = 1;
				s = p * 0.25f;
			}else{
				s = p / (2 * Mathf.PI) * Mathf.Asin(1 / a);
			}
			
			return (a * Mathf.Pow(2, -10 * value) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p) + 1);
		}		
		
		/* GFX47 MOD START */
		public static float easeInOutElastic(float value){
			float d = 1f;
			float p = d * .3f;
			float s = 0;
			float a = 0;
			
			if (value == 0) return 0;
			
			if ((value /= d*0.5f) == 2) return 1;
			
			if (a == 0f || a < 1){
				a = 1;
				s = p / 4;
			}else{
				s = p / (2 * Mathf.PI) * Mathf.Asin(1 / a);
			}
			
			if (value < 1) return -0.5f * (a * Mathf.Pow(2, 10 * (value-=1)) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p));
			return a * Mathf.Pow(2, -10 * (value-=1)) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p) * 0.5f + 1;
		}		
		/* GFX47 MOD END */
	}
	#endregion
}
