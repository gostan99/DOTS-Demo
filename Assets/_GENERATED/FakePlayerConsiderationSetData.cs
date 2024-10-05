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
		public BlobAssetReference<ConsiderationDefinition> RunSpeed;
		public BlobAssetReference<ConsiderationDefinition> NearestPickupDistance;
		public BlobAssetReference<ConsiderationDefinition> NearestAvoidDistance;
		public BlobAssetReference<ConsiderationDefinition> NearestAttackDistance;
		public BlobAssetReference<ConsiderationDefinition> WantAttack;
		public BlobAssetReference<ConsiderationDefinition> WantAvoid;
	}
	
	[CreateAssetMenu(menuName = "Trove/UtilityAI/ConsiderationSets/FakePlayerConsiderationSetData", fileName = "FakePlayerConsiderationSetData")]
	public class FakePlayerConsiderationSetData : ScriptableObject
	{
		[Header("Consideration Definitions")]
		public ConsiderationDefinitionAuthoring RunSpeed  = ConsiderationDefinitionAuthoring.GetDefault(0f, 1f);
		public ConsiderationDefinitionAuthoring NearestPickupDistance  = ConsiderationDefinitionAuthoring.GetDefault(0f, 1f);
		public ConsiderationDefinitionAuthoring NearestAvoidDistance  = ConsiderationDefinitionAuthoring.GetDefault(0f, 1f);
		public ConsiderationDefinitionAuthoring NearestAttackDistance  = ConsiderationDefinitionAuthoring.GetDefault(0f, 1f);
		public ConsiderationDefinitionAuthoring WantAttack  = ConsiderationDefinitionAuthoring.GetDefault(0f, 1f);
		public ConsiderationDefinitionAuthoring WantAvoid  = ConsiderationDefinitionAuthoring.GetDefault(0f, 1f);
		
		public void Bake(IBaker baker, out FakePlayerConsiderationSet considerationSetComponent)
		{
			considerationSetComponent = new FakePlayerConsiderationSet();
			considerationSetComponent.RunSpeed = RunSpeed.ToConsiderationDefinition(baker);
			considerationSetComponent.NearestPickupDistance = NearestPickupDistance.ToConsiderationDefinition(baker);
			considerationSetComponent.NearestAvoidDistance = NearestAvoidDistance.ToConsiderationDefinition(baker);
			considerationSetComponent.NearestAttackDistance = NearestAttackDistance.ToConsiderationDefinition(baker);
			considerationSetComponent.WantAttack = WantAttack.ToConsiderationDefinition(baker);
			considerationSetComponent.WantAvoid = WantAvoid.ToConsiderationDefinition(baker);
			baker.AddComponent(baker.GetEntity(TransformUsageFlags.None), considerationSetComponent);
		}
		
	}
}
