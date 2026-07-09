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
    <div className="shrink-0 text-center">
      <span className="block text-base font-black text-foreground">
        {priceFormatter.format(price)}
      </span>
      {previousPrice ? (
        <span className="block text-[0.68rem] font-semibold text-muted line-through">
          {priceFormatter.format(previousPrice)}
        </span>
      ) : null}
    </div>
  );
}
