using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class GushyBallPlayModeTests
{
    private const string SceneName = "NeonConnectWords";
    private const string BallName = "GushyBall";

    [UnityTest]
    public IEnumerator GushyBallSpawnsAndChangesColorWhenInteractedWith()
    {
        SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
        yield return null;
        yield return null;

        GameObject ballObject = GameObject.Find(BallName);
        Assert.That(ballObject, Is.Not.Null, "Expected the gushy ball to runtime-spawn into the NeonConnectWords scene.");

        Component ball = ballObject.GetComponent("GushyBallController");
        Assert.That(ball, Is.Not.Null, "Expected the spawned ball to have a GushyBallController.");

        Camera mainCamera = Camera.main;
        Assert.That(mainCamera, Is.Not.Null, "Expected the playable scene to have a main camera.");
        Collider ballCollider = ballObject.GetComponent<Collider>();
        Assert.That(ballCollider, Is.Not.Null, "Expected the gushy ball to have a collider for click detection.");

        PropertyInfo colorIndexProperty = ball.GetType().GetProperty("CurrentColorIndex");
        PropertyInfo targetColorProperty = ball.GetType().GetProperty("TargetVisualColor");
        PropertyInfo currentColorProperty = ball.GetType().GetProperty("CurrentVisualColor");
        MethodInfo interactionMethod = ball.GetType().GetMethod("TryInteractAtScreenPosition");

        Assert.That(colorIndexProperty, Is.Not.Null, "Expected the ball controller to expose its current color index for verification.");
        Assert.That(targetColorProperty, Is.Not.Null, "Expected the ball controller to expose its target color for verification.");
        Assert.That(currentColorProperty, Is.Not.Null, "Expected the ball controller to expose its visible color for verification.");
        Assert.That(interactionMethod, Is.Not.Null, "Expected the ball controller to expose a screen-space interaction method.");

        int initialColorIndex = (int)colorIndexProperty.GetValue(ball);
        Color initialTargetColor = (Color)targetColorProperty.GetValue(ball);
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(ballCollider.bounds.center);
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(ballCollider.bounds.center);
        Vector2 interactionPoint = new Vector2(screenPoint.x, screenPoint.y);

        Assert.That(screenPoint.z, Is.GreaterThan(0f), "Expected the ball to be visible to the main camera.");
        Assert.That(viewportPoint.x, Is.InRange(0.15f, 0.85f), "Expected the ball to spawn comfortably inside the horizontal camera view.");
        Assert.That(viewportPoint.y, Is.InRange(0.15f, 0.85f), "Expected the ball to spawn comfortably inside the vertical camera view.");
        Assert.That(Physics.Raycast(mainCamera.ScreenPointToRay(interactionPoint), out RaycastHit hitInfo, 100f), Is.True, "Expected the interaction ray to hit something in the scene.");
        Assert.That(hitInfo.collider != null && hitInfo.collider.gameObject == ballObject, Is.True, "Expected the interaction ray to hit the gushy ball collider.");
        Assert.That((bool)interactionMethod.Invoke(ball, new object[] { interactionPoint, -1 }), Is.True, "Expected the ball to accept a normal pointer interaction.");

        yield return new WaitForSeconds(0.2f);

        Assert.That((int)colorIndexProperty.GetValue(ball), Is.Not.EqualTo(initialColorIndex), "Expected interacting with the ball to advance the color index.");
        Assert.That(AreColorsDifferent((Color)targetColorProperty.GetValue(ball), initialTargetColor), Is.True, "Expected the interaction to change the ball target color.");
        Assert.That(AreColorsDifferent((Color)currentColorProperty.GetValue(ball), initialTargetColor), Is.True, "Expected the visible ball color to start transitioning after the interaction.");
    }

    private static bool AreColorsDifferent(Color a, Color b)
    {
        const float threshold = 0.01f;
        return Mathf.Abs(a.r - b.r) > threshold
            || Mathf.Abs(a.g - b.g) > threshold
            || Mathf.Abs(a.b - b.b) > threshold
            || Mathf.Abs(a.a - b.a) > threshold;
    }
}
