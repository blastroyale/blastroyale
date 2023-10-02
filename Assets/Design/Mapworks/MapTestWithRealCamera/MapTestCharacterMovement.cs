using UnityEngine;
using UnityEngine.InputSystem;

public class MapTestCharacterMovement : MonoBehaviour
{
	public float speed = 2.9f;

	private void Update()
	{
		if (Keyboard.current.rightArrowKey.isPressed)
		{
			transform.position += new Vector3(speed * Time.deltaTime, 0f, 0f);
		}

		if (Keyboard.current.leftArrowKey.isPressed)
		{
			transform.position -= new Vector3(speed * Time.deltaTime, 0f, 0f);
		}
		
		if (Keyboard.current.upArrowKey.isPressed)
		{
			transform.position += new Vector3(0f, 0f, speed * Time.deltaTime);
		}
		
		if (Keyboard.current.downArrowKey.isPressed)
		{
			transform.position -= new Vector3(0f, 0f, speed * Time.deltaTime);
		}
	}
}
