/// <summary>Un jefe que puede quedar aturdido (por ejemplo, tras un parry exitoso del jugador).</summary>
public interface IStunnable
{
    void Stun(float duration);
}
