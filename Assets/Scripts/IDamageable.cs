using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int damage);
}

public interface ICardEffectTarget : IDamageable
{
    void ApplySpade(int rank);
    void ApplyClub(int rank);
    void ApplyHeart(int rank);
    void ApplyDiamond(int rank);
}

public static class CardEffect
{
    public static int GetAmount(
        int rank,
        int baseValue,
        float jackRate,
        float queenRate,
        float kingRate
    )
    {
        if (rank <= 10)
        {
            return rank;
        }

        float rate = rank switch
        {
            11 => jackRate,
            12 => queenRate,
            13 => kingRate,
            _ => 0f
        };

        return Mathf.CeilToInt(baseValue * rate);
    }
}
