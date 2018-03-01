using System;

namespace SuperheroSerializer
{
    public class Gun
    {
        public int ammo = 10;
        public void Shot()
        {
            if (ammo-- > 0)
                Console.WriteLine("piu piu");
        }
    }
}
