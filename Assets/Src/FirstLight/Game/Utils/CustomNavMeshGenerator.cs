using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CustomNavMeshGenerator : MonoBehaviour
{
	public GameObject[] targetObjects;

	[Button("Generate Mesh")]
	public void CustomButtonClicked()
	{
		// Code to execute when the button is clicked
		var mesh = GenerateColliderMesh(targetObjects);
		var _meshFilter = GetComponent<MeshFilter>();

		_meshFilter.mesh = mesh;
	}

	Mesh GenerateColliderMesh(GameObject[] target)
	{
		// Loop through each target object to find it's box colliders, excluding triggers
		BoxCollider[] colliders;
		List<BoxCollider> colliderList = new List<BoxCollider>();
		foreach(var obj in target)
		{
			foreach(var box in obj.GetComponentsInChildren<BoxCollider>())
			{
				if (box.isTrigger)
				{
					continue;
				}
				colliderList.Add(box);
			}
		}

		colliders = colliderList.ToArray();

		// Create a combined bounds to encapsulate all colliders
		Bounds combinedBounds = new Bounds();
		foreach (BoxCollider collider in colliders)
		{
			combinedBounds.Encapsulate(GetColliderWorldBounds(collider));
		}

		// Create vertices and indices for the mesh
		Vector3[] vertices = new Vector3[colliders.Length * 8];
		int[] indices = new int[colliders.Length * 36];

		for (int i = 0; i < colliders.Length; i++)
		{
			BoxCollider collider = colliders[i];

			// Calculate the local bounds of the collider
			Bounds localBounds = GetColliderLocalBounds(collider);

			// Calculate the world position, rotation, and scale of the collider
			Vector3 worldPosition = collider.transform.position;
			Quaternion worldRotation = collider.transform.rotation;
			Vector3 worldScale = collider.transform.lossyScale;

			// Calculate the local vertices of the collider based on its local bounds
			Vector3[] localVertices = new Vector3[8];
			localVertices[0] = localBounds.center + new Vector3(-localBounds.extents.x, -localBounds.extents.y, -localBounds.extents.z);
			localVertices[1] = localBounds.center + new Vector3(localBounds.extents.x, -localBounds.extents.y, -localBounds.extents.z);
			localVertices[2] = localBounds.center + new Vector3(localBounds.extents.x, -localBounds.extents.y, localBounds.extents.z);
			localVertices[3] = localBounds.center + new Vector3(-localBounds.extents.x, -localBounds.extents.y, localBounds.extents.z);
			localVertices[4] = localBounds.center + new Vector3(-localBounds.extents.x, localBounds.extents.y, -localBounds.extents.z);
			localVertices[5] = localBounds.center + new Vector3(localBounds.extents.x, localBounds.extents.y, -localBounds.extents.z);
			localVertices[6] = localBounds.center + new Vector3(localBounds.extents.x, localBounds.extents.y, localBounds.extents.z);
			localVertices[7] = localBounds.center + new Vector3(-localBounds.extents.x, localBounds.extents.y, localBounds.extents.z);

			// Transform the local vertices to world space
			Vector3[] worldVertices = new Vector3[8];
			for (int j = 0; j < 8; j++)
			{
				worldVertices[j] = worldPosition + worldRotation * Vector3.Scale(localVertices[j], worldScale);
			}

			// Add the world vertices to the mesh
			int vertexIndex = i * 8;
			for (int j = 0; j < 8; j++)
			{
				vertices[vertexIndex + j] = worldVertices[j];
			}

			// Add the indices for the collider to the mesh
			int indexOffset = i * 36;
			int[] colliderIndices = new int[]
			{
				0, 1, 2, 2, 3, 0, // Bottom face
                1, 5, 6, 6, 2, 1, // Right face
                7, 6, 5, 5, 4, 7, // Top face
                4, 0, 3, 3, 7, 4, // Left face
                4, 5, 1, 1, 0, 4, // Back face
                3, 2, 6, 6, 7, 3  // Front face
            };
			for (int j = 0; j < 36; j++)
			{
				indices[indexOffset + j] = colliderIndices[j] + vertexIndex;
			}
		}

		// Create the mesh and assign the vertices and indices
		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.triangles = indices;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}

	Bounds GetColliderLocalBounds(BoxCollider collider)
	{
		Bounds bounds = new Bounds();
		bounds.center = collider.center;
		bounds.size = collider.size;
		return bounds;
	}

	Bounds GetColliderWorldBounds(BoxCollider collider)
	{
		Bounds localBounds = GetColliderLocalBounds(collider);
		Vector3 worldCenter = collider.transform.TransformPoint(localBounds.center);
		Vector3 worldExtents = collider.transform.TransformVector(localBounds.extents);
		Bounds worldBounds = new Bounds(worldCenter, worldExtents * 2);
		return worldBounds;
	}
}

