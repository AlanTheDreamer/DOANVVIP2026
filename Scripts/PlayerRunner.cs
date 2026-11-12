using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner
{
    /// <summary>
    /// Nhân vật chạy 3 làn. Hitbox (capsule) tách riêng khỏi model,
    /// nên thay model nào cũng không ảnh hưởng gameplay.
    /// Hierarchy: Player (Rigidbody, Capsule, script) → Visual → model của bạn
    /// </summary>
    public class PlayerRunner : MonoBehaviour
    {
        public const float StandHeight = 1.8f;
        public const float SlideHeight = 0.6f;
        public const float Radius = 0.35f;

        public float ForwardSpeed { get; set; }
        public bool IsDead { get; private set; }
        public bool IsGrounded { get; private set; } = true;
        public bool IsSliding => slideTimer > 0f;
        public event Action Died;
        public event Action<RunnerAction> Moved;

        RunnerGame game;
        RunnerTheme theme;
        CapsuleCollider capsule;
        Transform visualRoot;
        Animator anim;
        readonly HashSet<string> animParams = new HashSet<string>();

        int lane;
        float verticalVelocity;
        float slideTimer;
        float deathTimer;

        float JumpVelocity => Mathf.Sqrt(2f * game.jumpHeight * -game.gravity);
        bool HasSlideAnim => HasParam(theme.slideTrigger) || HasParam(theme.slidingParam);
        bool HasDeadAnim => HasParam(theme.deadTrigger);

        public void Setup(RunnerGame game, RunnerTheme theme)
        {
            this.game = game;
            this.theme = theme;

            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.radius = Radius;
            capsule.direction = 1;

            visualRoot = new GameObject("Visual").transform;
            visualRoot.SetParent(transform, false);

            if (theme.player.IsSet)
                ModelUtil.Attach(theme.player, visualRoot, new Vector3(0f, StandHeight, 0f), ModelUtil.Align.Bottom);
            else
                Placeholders.Player(visualRoot);

            anim = visualRoot.GetComponentInChildren<Animator>();
            if (anim != null && anim.runtimeAnimatorController == null) anim = null;
            if (anim != null)
                foreach (var p in anim.parameters) animParams.Add(p.name);

            SetHitbox(StandHeight);
        }

        public void ResetRunner()
        {
            IsDead = false;
            IsGrounded = true;
            ForwardSpeed = 0f;
            lane = 0;
            verticalVelocity = 0f;
            slideTimer = 0f;
            deathTimer = 0f;
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;
            SetHitbox(StandHeight);
            if (anim != null) { anim.Rebind(); anim.Update(0f); }
        }

        public void HandleAction(RunnerAction action)
        {
            if (IsDead) return;
            switch (action)
            {
                case RunnerAction.Left:
                    if (lane <= -1) return;
                    lane--;
                    break;
                case RunnerAction.Right:
                    if (lane >= 1) return;
                    lane++;
                    break;
                case RunnerAction.Jump:
                    if (!IsGrounded) return;
                    verticalVelocity = JumpVelocity;
                    IsGrounded = false;
                    slideTimer = 0f;
                    Trigger(theme.jumpTrigger);
                    break;
                case RunnerAction.Slide:
                    if (!IsGrounded) verticalVelocity = -JumpVelocity * 1.5f; // đang bay thì đập xuống nhanh
                    slideTimer = game.slideDuration;
                    Trigger(theme.slideTrigger);
                    break;
                default:
                    return;
            }
            Moved?.Invoke(action); // chỉ báo khi hành động thực sự xảy ra (dùng cho âm thanh)
        }

        public void Die()
        {
            if (IsDead) return;
            IsDead = true;
            ForwardSpeed = 0f;
            Trigger(theme.deadTrigger);
            Died?.Invoke();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 p = transform.position;

            // Trọng lực (vẫn rơi xuống đất kể cả khi đã chết)
            if (!IsGrounded)
            {
                verticalVelocity += game.gravity * dt;
                p.y += verticalVelocity * dt;
                if (p.y <= 0f) { p.y = 0f; verticalVelocity = 0f; IsGrounded = true; }
            }

            if (IsDead)
            {
                transform.position = p;
                if (!HasDeadAnim) // không có anim chết → ngã ngửa
                {
                    deathTimer += dt;
                    visualRoot.localRotation = Quaternion.Slerp(Quaternion.identity,
                        Quaternion.Euler(-90f, 0f, 0f), Mathf.Clamp01(deathTimer * 4f));
                }
                return;
            }

            p.z += ForwardSpeed * dt;
            float targetX = lane * game.laneWidth;
            p.x = Mathf.MoveTowards(p.x, targetX, game.laneChangeSpeed * dt);
            transform.position = p;

            if (slideTimer > 0f && IsGrounded) slideTimer -= dt;
            SetHitbox(IsSliding ? SlideHeight : StandHeight);

            // Nghiêng người khi đổi làn
            float lean = Mathf.Clamp(targetX - p.x, -1f, 1f) * -12f;
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, lean);

            // Không có anim trượt → bóp dẹt model
            bool squash = IsSliding && !HasSlideAnim;
            Vector3 targetScale = squash ? new Vector3(1.15f, 0.35f, 1.15f) : Vector3.one;
            visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, targetScale, 1f - Mathf.Exp(-20f * dt));

            if (anim != null)
            {
                if (HasParam(theme.speedParam)) anim.SetFloat(theme.speedParam, ForwardSpeed);
                if (HasParam(theme.groundedParam)) anim.SetBool(theme.groundedParam, IsGrounded);
                if (HasParam(theme.slidingParam)) anim.SetBool(theme.slidingParam, IsSliding);
            }
        }

        void SetHitbox(float height)
        {
            capsule.height = height;
            capsule.center = new Vector3(0f, height * 0.5f, 0f);
        }

        bool HasParam(string name) => anim != null && !string.IsNullOrEmpty(name) && animParams.Contains(name);

        void Trigger(string name)
        {
            if (HasParam(name)) anim.SetTrigger(name);
        }
    }
}
