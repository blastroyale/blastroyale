using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;

public class PopUpEffectAnimation
{
    private readonly VisualElement _element;
    private bool isPointerDown;

    public PopUpEffectAnimation(VisualElement element)
    {
        _element = element;

        Register();
    }

    public void Register()
    {
        _element.RegisterCallback<PointerDownEvent>(ev => ScaleDown(), TrickleDown.TrickleDown);
       	_element.RegisterCallback<PointerLeaveEvent>(ev => ScaleUp());
    }

    private void ScaleDown()
    {
        isPointerDown = true;
        _element.style.scale = new Scale(new Vector3(0.9f, 0.9f, 1));
    }

    private async void ScaleUp()
    {
        if (isPointerDown)
        {
            _element.style.scale = new Scale(new Vector3(1.1f, 1.1f, 1));
            await Task.Delay(150);
            ResetScale();
        }
    }

    private void ResetScale()
    {
        _element.style.scale = new Scale(new Vector3(1.0f, 1.0f, 1));
        isPointerDown = false;
    }
}