namespace EftDmaRadarLite.Tarkov.Data.TarkovMarket
{
    internal static class FleaTax
    {
        private const double CommunityItemTax = 3f;
        private const double CommunityRequirementTax = 3f;
        private const double RagFairCommissionModifier = 1f; // Constant in ItemTemplate

        /// <summary>
        /// Calculates the amount of tax to be paid on the flea market.
        /// </summary>
        /// <param name="requirementsPrice">Flea list price.</param>
        /// <param name="num">Base price value of the item.</param>
        /// <returns></returns>
        public static double Calculate(double requirementsPrice, double num) /// public static double CalculateTaxPrice(Item item, int offerItemCount, double requirementsPrice, bool sellInOnePiece)
        {
            if (num == 0d || requirementsPrice == 0d) // Prevent DIV by Zero
            {
                return 0d;
            }
            // Convert tax modifiers to percentages
            double num2 = CommunityItemTax / 100d;
            double num3 = CommunityRequirementTax / 100d;
            double num4 = Math.Log10(num / requirementsPrice);
            double num5 = Math.Log10(requirementsPrice / num);

            // Determine the logarithm to weight based on relationship to the item's base price
            if (requirementsPrice >= num)
            {
                num5 = Math.Pow(num5, 1.08d);
            }
            else
            {
                num4 = Math.Pow(num4, 1.08d);
            }
            num4 = Math.Pow(4.0d, num4);
            num5 = Math.Pow(4.0d, num5);

            // Get the base tax amount
            double num6 = num * num2 * num4 + requirementsPrice * num3 * num5;

            // Apply the RagFairCommissionModifier
            return num6 * RagFairCommissionModifier;
        }
    }
}
