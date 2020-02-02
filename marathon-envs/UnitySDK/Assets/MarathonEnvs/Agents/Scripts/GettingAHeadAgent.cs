using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;
using static BodyHelper002;

public class GettingAHeadAgent : Agent, IOnTerrainCollision
{
	BodyManager002 _bodyManager;

	public bool MoveRight = true;
	public bool MoveLeft;
	public bool Jump;
	public int StepsUntilChange;


	bool _isTrainingMode = false;

	override public void CollectObservations()
	{
		Vector3 normalizedVelocity = _bodyManager.GetNormalizedVelocity();
        var pelvis = _bodyManager.GetFirstBodyPart(BodyPartGroup.Torso);
        var shoulders = _bodyManager.GetFirstBodyPart(BodyPartGroup.Torso);

        AddVectorObs(normalizedVelocity);
		AddVectorObs(pelvis.Rigidbody.transform.forward); // gyroscope 
		AddVectorObs(pelvis.Rigidbody.transform.up);

		//AddVectorObs(_bodyManager.GetSensorIsInTouch());
		var sensorsInTouch = _bodyManager.GetSensorIsInTouch();
		AddVectorObs(sensorsInTouch);

		// JointRotations.ForEach(x => AddVectorObs(x)); = 6*4 = 24
		var jointRotations = _bodyManager.GetMusclesRotations();
		jointRotations.ForEach(x => AddVectorObs(x));

		// AddVectorObs(JointVelocity); = 6
		var jointVelocity = _bodyManager.GetMusclesObservations();
		AddVectorObs(jointVelocity);

		// AddVectorObs.  = 2
		var feetHeight = _bodyManager.GetSensorZPositions();
		AddVectorObs(feetHeight);

        AddVectorObs(MoveLeft);
        AddVectorObs(MoveRight);
        AddVectorObs(Jump);

        _bodyManager.OnCollectObservationsHandleDebug(GetInfo());
	}

	public override void AgentAction(float[] vectorAction, string textAction)
	{
		if (_isTrainingMode)
			HandleControllerTraining();
		// apply actions to body
		_bodyManager.OnAgentAction(vectorAction, textAction);

		// manage reward
        float velocity = Mathf.Clamp(_bodyManager.GetNormalizedVelocity().x, 0f, 1f);
		var actionDifference = _bodyManager.GetActionDifference();
		var actionsAbsolute = vectorAction.Select(x=>Mathf.Abs(x)).ToList();
		var actionsAtLimit = actionsAbsolute.Select(x=> x>=1f ? 1f : 0f).ToList();
		float actionaAtLimitCount = actionsAtLimit.Sum();
        float notAtLimitBonus = 1f - (actionaAtLimitCount / (float) actionsAbsolute.Count);
        float reducedPowerBonus = 1f - actionsAbsolute.Average();
	
        var pelvis = _bodyManager.GetFirstBodyPart(BodyPartGroup.Torso);
		if (pelvis.Transform.position.y<0){
			Done();
		}

		bool goalStationary = false;
		bool goalRight = false;
		float reward = 0f;
		if (MoveRight && MoveLeft)
			goalStationary = true;
		else if (!MoveRight && !MoveLeft)
			goalStationary = true;
		else if (MoveRight)
			goalRight = true;

		var sensorsInTouch = _bodyManager.GetSensorIsInTouch();
		var anySensorInTouch = sensorsInTouch.Sum() != 0;
		var feetHeights = _bodyManager.GetSensorZPositions();
		var footHeight = feetHeights.Min();
		var jumpReward = 0f;
		if (!anySensorInTouch)
		{
			jumpReward += .05f;
			jumpReward += footHeight;
		}

		if (goalStationary)
		{
			velocity = Mathf.Abs(velocity);
			reward = 1f - velocity;
			reward = Mathf.Clamp(reward, -1f, 1f);
		}
		else if (goalRight)
			reward = velocity;
		else
			reward = -velocity;
        if (Jump)
        {
			reward = reward * .5f;
			reward += (jumpReward * .5f);
        }
		reward = Mathf.Clamp(reward, -1f, 1f);

		AddReward(reward);
		_bodyManager.SetDebugFrameReward(reward);
	}


	public override void AgentReset()
	{
		if (_bodyManager == null)
		{
			_bodyManager = GetComponent<BodyManager002>();
			var academy = FindObjectOfType<Academy>();
			_isTrainingMode = academy.agentSpawner.trainingMode;
		}
		_bodyManager.OnAgentReset();
		//StepsUntilChange = 0;
		//SetAction(0);
	}
	public virtual void OnTerrainCollision(GameObject other, GameObject terrain)
	{
		// if (string.Compare(terrain.name, "Terrain", true) != 0)
		if (terrain.GetComponent<Terrain>() == null)
			return;
		// if (!_styleAnimator.AnimationStepsReady)
		// 	return;
		var bodyPart = _bodyManager.BodyParts.FirstOrDefault(x=>x.Transform.gameObject == other);
		if (bodyPart == null)
			return;
		switch (bodyPart.Group)
		{
			case BodyHelper002.BodyPartGroup.None:
			case BodyHelper002.BodyPartGroup.Foot:
			// case BodyHelper002.BodyPartGroup.LegUpper:
			case BodyHelper002.BodyPartGroup.LegLower:
			case BodyHelper002.BodyPartGroup.Hand:
			// case BodyHelper002.BodyPartGroup.ArmLower:
			// case BodyHelper002.BodyPartGroup.ArmUpper:
				break;
			default:
				// AddReward(-100f);
				if (!IsDone()){
					Done();
				}
				break;
		}
	}
    void HandleControllerTraining()
    {
		StepsUntilChange--;
		if (StepsUntilChange > 0)
			return;
		var rnd = UnityEngine.Random.value;
		bool repeateAction = false;
		int action = AsAction();
		if (action != 0 && rnd > .6f)
			repeateAction = true;
		if (!repeateAction)
		{
			rnd = UnityEngine.Random.value;
			if (rnd <= .4f)
				action = 1; // right
			else if (rnd <= .8f)
				action = 2; // left
			else
				action = 0; // stand
			rnd = UnityEngine.Random.value;
			if (rnd >= .75)
				action += 3; // add jump
		}
		StepsUntilChange = 40 + (int)(UnityEngine.Random.value * 200);
		SetAction(action);
	}
    int AsAction()
    {
		int action = 0;
		if (MoveRight && MoveLeft)
			action = 0;
		else if (!MoveRight && !MoveLeft)
			action = 0;
		else if (MoveRight)
			action = 1;
        else
			action = 2;
		if (Jump)
			action += 3;
		return action;
	}
    void SetAction(int action)
    {
		Jump = false;
        if (action >=3)
        {
			action -= 3;
			Jump = true;
        }
		MoveRight = true ? action == 1 : false;
		MoveLeft = true ? action == 2 : false;
    }
}
