using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using System;
using System.Collections.Generic;
using Trove;
using Trove.UtilityAI;
using Action = Trove.UtilityAI.Action;

namespace _GENERATED
{
	[Serializable]
	public struct FakePlayerConsiderationSet : IComponentData
	{
		public BlobAssetReference<ConsiderationDefinition> SwordKineticEnergy;
		public BlobAssetReference<ConsiderationDefinition> RunSpeed;
		public BlobAssetReference<ConsiderationDefinition> NearestActor;
		public BlobAssetReference<ConsiderationDefinition> NearestPickup;
		public BlobAssetReference<ConsiderationDefinition> NearestActorAvoidance;
	}
	
	[CreateAssetMenu(menuName = "Trove/UtilityAI/ConsiderationSets/FakePlayerConsiderationSetData", fileName = "FakePlayerConsiderationSetData")]
	public class FakePlayerConsiderationSetData : ScriptableObject
	{
		[Header("Consideration Definitions")]
		public ConsiderationDefinitionAuthoring SwordKineticEnergy  = ConsiderationDefinitionAuthoring.GetDefault(0f, 1f);
		public ConsiderationDefinitionAuthoring RunSpeed  = ConsiderationDefinitionAuthoring.GetDefault(0f, 1f);
		public ConsiderationDefinitionAuthoring NearestActor  = ConsiderationDefinitionAuthoring.GetDefault(0f, 1f);
		public ConsiderationDefinitionAuthoring NearestPickup  = ConsiderationDefinitionAuthoring.GetDefault(0f, 1f);
		public ConsiderationDefinitionAuthoring NearestActorAvoidance  = ConsiderationDefinitionAuthoring.GetDefault(0f, 1f);
		
		public void Bake(IBaker baker, out FakePlayerConsiderationSet considerationSetComponent)
		{
			considerationSetComponent = new FakePlayerConsiderationSet();
			considerationSetComponent.SwordKineticEnergy = SwordKineticEnergy.ToConsiderationDefinition(baker);
			considerationSetComponent.RunSpeed = RunSpeed.ToConsiderationDefinition(baker);
			considerationSetComponent.NearestActor = NearestActor.ToConsiderationDefinition(baker);
			considerationSetComponent.NearestPickup = NearestPickup.ToConsiderationDefinition(baker);
			considerationSetComponent.NearestActorAvoidance = NearestActorAvoidance.ToConsiderationDefinition(baker);
			baker.AddComponent(baker.GetEntity(TransformUsageFlags.None), considerationSetComponent);
		}
		
	}
}
