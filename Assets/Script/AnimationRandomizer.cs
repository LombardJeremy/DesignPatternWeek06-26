using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomStartAnimation : MonoBehaviour
{
    [SerializeField] Animation _animationComponent;
    string _clipName = "Light Random";

    void Reset()
    {
        _animationComponent = GetComponent<Animation>();
    }

    void Start()
    {
        AnimationState state = _animationComponent[_clipName];
        float randomTime = Random.Range(0f, state.length);
        state.time = randomTime;
        _animationComponent.Play(_clipName);
    }
}