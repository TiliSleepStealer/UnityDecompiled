using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
	internal class CollisionModuleUI : ModuleUI
	{
		private enum CollisionTypes
		{
			Plane,
			World
		}

		private enum CollisionModes
		{
			Mode3D,
			Mode2D
		}

		private enum PlaneVizType
		{
			Grid,
			Solid
		}

		private class Texts
		{
			public GUIContent lifetimeLoss = new GUIContent("Lifetime Loss", "When particle collides, it will lose this fraction of its Start Lifetime");

			public GUIContent planes = new GUIContent("Planes", "Planes are defined by assigning a reference to a transform. This transform can be any transform in the scene and can be animated. Multiple planes can be used. Note: the Y-axis is used as the plane normal.");

			public GUIContent createPlane = new GUIContent(string.Empty, "Create an empty GameObject and assign it as a plane.");

			public GUIContent minKillSpeed = new GUIContent("Min Kill Speed", "When particles collide and their speed is lower than this value, they are killed.");

			public GUIContent dampen = new GUIContent("Dampen", "When particle collides, it will lose this fraction of its speed. Unless this is set to 0.0, particle will become slower after collision.");

			public GUIContent bounce = new GUIContent("Bounce", "When particle collides, the bounce is scaled with this value. The bounce is the upwards motion in the plane normal direction.");

			public GUIContent radiusScale = new GUIContent("Radius Scale", "Scale particle bounds by this amount to get more precise collisions.");

			public GUIContent visualization = new GUIContent("Visualization", "Only used for visualizing the planes: Wireframe or Solid.");

			public GUIContent scalePlane = new GUIContent("Scale Plane", "Resizes the visualization planes.");

			public GUIContent visualizeBounds = new GUIContent("Visualize Bounds", "Render the collision bounds of the particles.");

			public GUIContent collidesWith = new GUIContent("Collides With", "Collides the particles with colliders included in the layermask.");

			public GUIContent collidesWithDynamic = new GUIContent("Enable Dynamic Colliders", "Should particles collide with dynamic objects?");

			public GUIContent interiorCollisions = new GUIContent("Interior Collisions", "Should particles collide with the insides of objects?");

			public GUIContent maxCollisionShapes = new GUIContent("Max Collision Shapes", "How many collision shapes can be considered for particle collisions. Excess shapes will be ignored. Terrains take priority.");

			public GUIContent quality = new GUIContent("Collision Quality", "Quality of world collisions. Medium and low quality are approximate and may leak particles.");

			public string[] qualitySettings = new string[]
			{
				"High",
				"Medium",
				"Low"
			};

			public GUIContent voxelSize = new GUIContent("Voxel Size", "Size of voxels in the collision cache.");

			public GUIContent collisionMessages = new GUIContent("Send Collision Messages", "Send collision callback messages.");

			public GUIContent collisionMode = new GUIContent("Collision Mode", "Use 3D Physics or 2D Physics.");
		}

		private const int k_MaxNumPlanes = 6;

		private string[] m_PlaneVizTypeNames = new string[]
		{
			"Grid",
			"Solid"
		};

		private SerializedProperty m_Type;

		private SerializedProperty[] m_Planes = new SerializedProperty[6];

		private SerializedMinMaxCurve m_Dampen;

		private SerializedMinMaxCurve m_Bounce;

		private SerializedMinMaxCurve m_LifetimeLossOnCollision;

		private SerializedProperty m_MinKillSpeed;

		private SerializedProperty m_RadiusScale;

		private SerializedProperty m_CollidesWith;

		private SerializedProperty m_CollidesWithDynamic;

		private SerializedProperty m_InteriorCollisions;

		private SerializedProperty m_MaxCollisionShapes;

		private SerializedProperty m_Quality;

		private SerializedProperty m_VoxelSize;

		private SerializedProperty m_CollisionMessages;

		private SerializedProperty m_CollisionMode;

		private CollisionModuleUI.PlaneVizType m_PlaneVisualizationType = CollisionModuleUI.PlaneVizType.Solid;

		private SerializedProperty[] m_ShownPlanes;

		private float m_ScaleGrid = 1f;

		private bool m_VisualizeBounds;

		private static Transform m_SelectedTransform;

		private static CollisionModuleUI s_LastInteractedEditor;

		private static CollisionModuleUI.Texts s_Texts;

		public CollisionModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName) : base(owner, o, "CollisionModule", displayName)
		{
			this.m_ToolTip = "Allows you to specify multiple collision planes that the particle can collide with.";
		}

		protected override void Init()
		{
			if (this.m_Type != null)
			{
				return;
			}
			if (CollisionModuleUI.s_Texts == null)
			{
				CollisionModuleUI.s_Texts = new CollisionModuleUI.Texts();
			}
			this.m_Type = base.GetProperty("type");
			List<SerializedProperty> list = new List<SerializedProperty>();
			for (int i = 0; i < this.m_Planes.Length; i++)
			{
				this.m_Planes[i] = base.GetProperty("plane" + i);
				if (i == 0 || this.m_Planes[i].objectReferenceValue != null)
				{
					list.Add(this.m_Planes[i]);
				}
			}
			this.m_ShownPlanes = list.ToArray();
			this.m_Dampen = new SerializedMinMaxCurve(this, CollisionModuleUI.s_Texts.dampen, "m_Dampen");
			this.m_Dampen.m_AllowCurves = false;
			this.m_Bounce = new SerializedMinMaxCurve(this, CollisionModuleUI.s_Texts.bounce, "m_Bounce");
			this.m_Bounce.m_AllowCurves = false;
			this.m_LifetimeLossOnCollision = new SerializedMinMaxCurve(this, CollisionModuleUI.s_Texts.lifetimeLoss, "m_EnergyLossOnCollision");
			this.m_LifetimeLossOnCollision.m_AllowCurves = false;
			this.m_MinKillSpeed = base.GetProperty("minKillSpeed");
			this.m_RadiusScale = base.GetProperty("radiusScale");
			this.m_PlaneVisualizationType = (CollisionModuleUI.PlaneVizType)EditorPrefs.GetInt("PlaneColisionVizType", 1);
			this.m_ScaleGrid = EditorPrefs.GetFloat("ScalePlaneColision", 1f);
			this.m_VisualizeBounds = EditorPrefs.GetBool("VisualizeBounds", false);
			this.m_CollidesWith = base.GetProperty("collidesWith");
			this.m_CollidesWithDynamic = base.GetProperty("collidesWithDynamic");
			this.m_InteriorCollisions = base.GetProperty("interiorCollisions");
			this.m_MaxCollisionShapes = base.GetProperty("maxCollisionShapes");
			this.m_Quality = base.GetProperty("quality");
			this.m_VoxelSize = base.GetProperty("voxelSize");
			this.m_CollisionMessages = base.GetProperty("collisionMessages");
			this.m_CollisionMode = base.GetProperty("collisionMode");
			this.SyncVisualization();
		}

		protected override void SetVisibilityState(ModuleUI.VisibilityState newState)
		{
			base.SetVisibilityState(newState);
			if (newState != ModuleUI.VisibilityState.VisibleAndFoldedOut)
			{
				Tools.s_Hidden = false;
				CollisionModuleUI.m_SelectedTransform = null;
				ParticleEffectUtils.ClearPlanes();
			}
			else
			{
				this.SyncVisualization();
			}
		}

		public override void OnInspectorGUI(ParticleSystem s)
		{
			if (CollisionModuleUI.s_Texts == null)
			{
				CollisionModuleUI.s_Texts = new CollisionModuleUI.Texts();
			}
			string[] options = new string[]
			{
				"Planes",
				"World"
			};
			CollisionModuleUI.CollisionTypes collisionTypes = (CollisionModuleUI.CollisionTypes)ModuleUI.GUIPopup(string.Empty, this.m_Type, options);
			CollisionModuleUI.CollisionModes collisionModes = CollisionModuleUI.CollisionModes.Mode3D;
			if (collisionTypes == CollisionModuleUI.CollisionTypes.Plane)
			{
				this.DoListOfPlanesGUI();
				EditorGUI.BeginChangeCheck();
				this.m_PlaneVisualizationType = (CollisionModuleUI.PlaneVizType)ModuleUI.GUIPopup(CollisionModuleUI.s_Texts.visualization, (int)this.m_PlaneVisualizationType, this.m_PlaneVizTypeNames);
				if (EditorGUI.EndChangeCheck())
				{
					EditorPrefs.SetInt("PlaneColisionVizType", (int)this.m_PlaneVisualizationType);
					if (this.m_PlaneVisualizationType == CollisionModuleUI.PlaneVizType.Solid)
					{
						this.SyncVisualization();
					}
					else
					{
						ParticleEffectUtils.ClearPlanes();
					}
				}
				EditorGUI.BeginChangeCheck();
				this.m_ScaleGrid = ModuleUI.GUIFloat(CollisionModuleUI.s_Texts.scalePlane, this.m_ScaleGrid, "f2");
				if (EditorGUI.EndChangeCheck())
				{
					this.m_ScaleGrid = Mathf.Max(0f, this.m_ScaleGrid);
					EditorPrefs.SetFloat("ScalePlaneColision", this.m_ScaleGrid);
					this.SyncVisualization();
				}
			}
			else
			{
				collisionModes = (CollisionModuleUI.CollisionModes)ModuleUI.GUIPopup(CollisionModuleUI.s_Texts.collisionMode, this.m_CollisionMode, new string[]
				{
					"3D",
					"2D"
				});
			}
			EditorGUI.BeginChangeCheck();
			this.m_VisualizeBounds = ModuleUI.GUIToggle(CollisionModuleUI.s_Texts.visualizeBounds, this.m_VisualizeBounds);
			if (EditorGUI.EndChangeCheck())
			{
				EditorPrefs.SetBool("VisualizeBounds", this.m_VisualizeBounds);
			}
			CollisionModuleUI.s_LastInteractedEditor = this;
			ModuleUI.GUIMinMaxCurve(CollisionModuleUI.s_Texts.dampen, this.m_Dampen);
			ModuleUI.GUIMinMaxCurve(CollisionModuleUI.s_Texts.bounce, this.m_Bounce);
			ModuleUI.GUIMinMaxCurve(CollisionModuleUI.s_Texts.lifetimeLoss, this.m_LifetimeLossOnCollision);
			ModuleUI.GUIFloat(CollisionModuleUI.s_Texts.minKillSpeed, this.m_MinKillSpeed);
			if (collisionTypes != CollisionModuleUI.CollisionTypes.World || collisionModes == CollisionModuleUI.CollisionModes.Mode2D)
			{
				ModuleUI.GUIFloat(CollisionModuleUI.s_Texts.radiusScale, this.m_RadiusScale);
			}
			if (collisionTypes == CollisionModuleUI.CollisionTypes.World)
			{
				ModuleUI.GUILayerMask(CollisionModuleUI.s_Texts.collidesWith, this.m_CollidesWith);
				ModuleUI.GUIToggle(CollisionModuleUI.s_Texts.collidesWithDynamic, this.m_CollidesWithDynamic);
				if (collisionModes == CollisionModuleUI.CollisionModes.Mode3D)
				{
					ModuleUI.GUIToggle(CollisionModuleUI.s_Texts.interiorCollisions, this.m_InteriorCollisions);
				}
				ModuleUI.GUIInt(CollisionModuleUI.s_Texts.maxCollisionShapes, this.m_MaxCollisionShapes);
				ModuleUI.GUIPopup(CollisionModuleUI.s_Texts.quality, this.m_Quality, CollisionModuleUI.s_Texts.qualitySettings);
				if (this.m_Quality.intValue > 0)
				{
					ModuleUI.GUIFloat(CollisionModuleUI.s_Texts.voxelSize, this.m_VoxelSize);
				}
			}
			ModuleUI.GUIToggle(CollisionModuleUI.s_Texts.collisionMessages, this.m_CollisionMessages);
		}

		protected override void OnModuleEnable()
		{
			base.OnModuleEnable();
			this.SyncVisualization();
		}

		protected override void OnModuleDisable()
		{
			base.OnModuleDisable();
			ParticleEffectUtils.ClearPlanes();
		}

		private void SyncVisualization()
		{
			if (!base.enabled)
			{
				return;
			}
			if (this.m_PlaneVisualizationType != CollisionModuleUI.PlaneVizType.Solid)
			{
				return;
			}
			for (int i = 0; i < this.m_ShownPlanes.Length; i++)
			{
				UnityEngine.Object objectReferenceValue = this.m_ShownPlanes[i].objectReferenceValue;
				if (!(objectReferenceValue == null))
				{
					Transform transform = objectReferenceValue as Transform;
					if (!(transform == null))
					{
						GameObject plane = ParticleEffectUtils.GetPlane(i);
						plane.transform.position = transform.position;
						plane.transform.rotation = transform.rotation;
						plane.transform.localScale = new Vector3(this.m_ScaleGrid, this.m_ScaleGrid, this.m_ScaleGrid);
						plane.transform.position += transform.up.normalized * 0.002f;
					}
				}
			}
		}

		private static GameObject CreateEmptyGameObject(string name, ParticleSystem parentOfGameObject)
		{
			GameObject gameObject = new GameObject(name);
			if (gameObject)
			{
				if (parentOfGameObject)
				{
					gameObject.transform.parent = parentOfGameObject.transform;
				}
				return gameObject;
			}
			return null;
		}

		private void DoListOfPlanesGUI()
		{
			int num = base.GUIListOfFloatObjectToggleFields(CollisionModuleUI.s_Texts.planes, this.m_ShownPlanes, null, CollisionModuleUI.s_Texts.createPlane, true);
			if (num >= 0)
			{
				GameObject gameObject = CollisionModuleUI.CreateEmptyGameObject("Plane Transform " + (num + 1), this.m_ParticleSystemUI.m_ParticleSystem);
				gameObject.transform.localPosition = new Vector3(0f, 0f, (float)(10 + num));
				gameObject.transform.localEulerAngles = new Vector3(-90f, 0f, 0f);
				this.m_ShownPlanes[num].objectReferenceValue = gameObject;
				this.SyncVisualization();
			}
			Rect rect = GUILayoutUtility.GetRect(0f, 16f);
			rect.x = rect.xMax - 24f - 5f;
			rect.width = 12f;
			if (this.m_ShownPlanes.Length > 1 && ModuleUI.MinusButton(rect))
			{
				this.m_ShownPlanes[this.m_ShownPlanes.Length - 1].objectReferenceValue = null;
				List<SerializedProperty> list = new List<SerializedProperty>(this.m_ShownPlanes);
				list.RemoveAt(list.Count - 1);
				this.m_ShownPlanes = list.ToArray();
			}
			if (this.m_ShownPlanes.Length < 6)
			{
				rect.x += 17f;
				if (ModuleUI.PlusButton(rect))
				{
					List<SerializedProperty> list2 = new List<SerializedProperty>(this.m_ShownPlanes);
					list2.Add(this.m_Planes[list2.Count]);
					this.m_ShownPlanes = list2.ToArray();
				}
			}
		}

		public override void OnSceneGUI(ParticleSystem s, InitialModuleUI initial)
		{
			Event current = Event.current;
			EventType eventType = current.type;
			if (current.type == EventType.Ignore && current.rawType == EventType.MouseUp)
			{
				eventType = current.rawType;
			}
			Color color = Handles.color;
			Color color2 = new Color(1f, 1f, 1f, 0.5f);
			Handles.color = color2;
			if (this.m_Type.intValue == 0)
			{
				for (int i = 0; i < this.m_ShownPlanes.Length; i++)
				{
					UnityEngine.Object objectReferenceValue = this.m_ShownPlanes[i].objectReferenceValue;
					if (objectReferenceValue != null)
					{
						Transform transform = objectReferenceValue as Transform;
						if (transform != null)
						{
							Vector3 position = transform.position;
							Quaternion rotation = transform.rotation;
							Vector3 axis = rotation * Vector3.right;
							Vector3 normal = rotation * Vector3.up;
							Vector3 axis2 = rotation * Vector3.forward;
							if (object.ReferenceEquals(CollisionModuleUI.m_SelectedTransform, transform))
							{
								Tools.s_Hidden = true;
								EditorGUI.BeginChangeCheck();
								if (Tools.current == Tool.Move)
								{
									transform.position = Handles.PositionHandle(position, rotation);
								}
								else if (Tools.current == Tool.Rotate)
								{
									transform.rotation = Handles.RotationHandle(rotation, position);
								}
								if (EditorGUI.EndChangeCheck())
								{
									if (this.m_PlaneVisualizationType == CollisionModuleUI.PlaneVizType.Solid)
									{
										GameObject plane = ParticleEffectUtils.GetPlane(i);
										plane.transform.position = position;
										plane.transform.rotation = rotation;
										plane.transform.localScale = new Vector3(this.m_ScaleGrid, this.m_ScaleGrid, this.m_ScaleGrid);
									}
									ParticleSystemEditorUtils.PerformCompleteResimulation();
								}
							}
							else
							{
								int keyboardControl = GUIUtility.keyboardControl;
								float size = HandleUtility.GetHandleSize(position) * 0.06f;
								Handles.FreeMoveHandle(position, Quaternion.identity, size, Vector3.zero, new Handles.DrawCapFunction(Handles.RectangleCap));
								if (eventType == EventType.MouseDown && current.type == EventType.Used && keyboardControl != GUIUtility.keyboardControl)
								{
									CollisionModuleUI.m_SelectedTransform = transform;
									eventType = EventType.Used;
								}
							}
							if (this.m_PlaneVisualizationType == CollisionModuleUI.PlaneVizType.Grid)
							{
								Color color3 = Handles.s_ColliderHandleColor * 0.9f;
								if (!base.enabled)
								{
									color3 = new Color(0.7f, 0.7f, 0.7f, 0.7f);
								}
								this.DrawGrid(position, axis, axis2, normal, color3, i);
							}
							else
							{
								this.DrawSolidPlane(position, rotation, i);
							}
						}
						else
						{
							Debug.LogError("Not a transform: " + objectReferenceValue.GetType());
						}
					}
				}
			}
			Handles.color = color;
		}

		[DrawGizmo(GizmoType.Active)]
		private static void RenderCollisionBounds(ParticleSystem system, GizmoType gizmoType)
		{
			if (CollisionModuleUI.s_LastInteractedEditor == null)
			{
				return;
			}
			if (!CollisionModuleUI.s_LastInteractedEditor.m_VisualizeBounds)
			{
				return;
			}
			if (CollisionModuleUI.s_LastInteractedEditor.m_ParticleSystemUI.m_ParticleSystem != system)
			{
				return;
			}
			ParticleSystem.Particle[] array = new ParticleSystem.Particle[system.particleCount];
			int particles = system.GetParticles(array);
			Color color = Gizmos.color;
			Gizmos.color = Color.green;
			Matrix4x4 matrix4x = Matrix4x4.identity;
			if (system.simulationSpace == ParticleSystemSimulationSpace.Local)
			{
				matrix4x = system.transform.localToWorldMatrix;
			}
			for (int i = 0; i < particles; i++)
			{
				ParticleSystem.Particle particle = array[i];
				Gizmos.DrawWireSphere(matrix4x.MultiplyPoint(particle.position), particle.GetCurrentSize(system) * 0.5f * CollisionModuleUI.s_LastInteractedEditor.m_RadiusScale.floatValue);
			}
			Gizmos.color = color;
		}

		private void DrawSolidPlane(Vector3 pos, Quaternion rot, int planeIndex)
		{
		}

		private void DrawGrid(Vector3 pos, Vector3 axis1, Vector3 axis2, Vector3 normal, Color color, int planeIndex)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			HandleUtility.ApplyWireMaterial();
			if (color.a > 0f)
			{
				GL.Begin(1);
				float num = 10f;
				num *= this.m_ScaleGrid;
				int num2 = (int)num;
				num2 = Mathf.Clamp(num2, 10, 40);
				if (num2 % 2 == 0)
				{
					num2++;
				}
				float d = num * 0.5f;
				float d2 = num / (float)(num2 - 1);
				Vector3 b = axis1 * num;
				Vector3 b2 = axis2 * num;
				Vector3 a = axis1 * d2;
				Vector3 a2 = axis2 * d2;
				Vector3 a3 = pos - axis1 * d - axis2 * d;
				for (int i = 0; i < num2; i++)
				{
					if (i % 2 == 0)
					{
						GL.Color(color * 0.7f);
					}
					else
					{
						GL.Color(color);
					}
					GL.Vertex(a3 + (float)i * a);
					GL.Vertex(a3 + (float)i * a + b2);
					GL.Vertex(a3 + (float)i * a2);
					GL.Vertex(a3 + (float)i * a2 + b);
				}
				GL.Color(color);
				GL.Vertex(pos);
				GL.Vertex(pos + normal);
				GL.End();
			}
		}

		public override void UpdateCullingSupportedString(ref string text)
		{
			text += "\n\tCollision is enabled.";
		}
	}
}
