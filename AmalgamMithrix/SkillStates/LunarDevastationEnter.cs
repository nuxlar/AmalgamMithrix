using EntityStates.BrotherMonster;

public class LunarDevastationEnter : UltEnterState
{

  public override void FixedUpdate()
  {
    base.FixedUpdate();
    if (!this.isAuthority || (double)this.fixedAge <= (double)UltEnterState.duration)
      return;
    this.outer.SetNextState(new LunarDevastationChannel());
  }
}