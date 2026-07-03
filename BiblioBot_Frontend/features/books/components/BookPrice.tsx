import { defaultPriceLocale, priceFormatOptions } from "@/constants/currency";

type BookPriceProps = {
  price: number;
  previousPrice?: number;
};

const priceFormatter = new Intl.NumberFormat(
  defaultPriceLocale,
  priceFormatOptions,
);

export function BookPrice({ price, previousPrice }: BookPriceProps) {
  return (
    <div className="flex items-baseline gap-2">
      <span className="text-lg font-semibold text-foreground">
        {priceFormatter.format(price)}
      </span>
      {previousPrice ? (
        <span className="text-sm text-muted line-through">
          {priceFormatter.format(previousPrice)}
        </span>
      ) : null}
    </div>
  );
}
