"use client";

import { motion, useReducedMotion } from "framer-motion";
import type { MotionStyle } from "framer-motion";
import Image from "next/image";

type FloatingBookProps = {
  className?: string;
  style?: MotionStyle;
};

export function FloatingBook({ className, style }: FloatingBookProps) {
  const shouldReduceMotion = useReducedMotion();

  return (
    <motion.div
      aria-hidden="true"
      className={className}
      style={style}
      initial={false}
    >
      <motion.div
        className="relative mx-auto flex aspect-[4/5] w-full items-center justify-center"
        animate={
          shouldReduceMotion
            ? { translateY: 0, rotateZ: 0 }
            : { translateY: [0, -14, 0], rotateZ: [-1.5, 1, -1.5] }
        }
        transition={{
          duration: 6.8,
          ease: "easeInOut",
          repeat: Infinity,
        }}
      >
        <div className="absolute inset-3 rounded-full bg-accent/20 blur-3xl sm:inset-6" />
        <div className="absolute -right-6 top-10 h-32 w-32 rounded-full border border-paper/15 bg-white/[0.04] backdrop-blur sm:-right-8 sm:h-40 sm:w-40" />
        <div className="absolute bottom-14 left-0 h-24 w-24 rounded-full border border-accent/20 bg-accent/10 blur-sm sm:h-28 sm:w-28" />
        <div className="absolute bottom-2 left-1/2 h-20 w-[78%] -translate-x-1/2 rounded-full bg-black/35 blur-2xl sm:h-24" />
        <div className="relative h-full w-full drop-shadow-[0_38px_55px_rgba(0,0,0,0.42)]">
          <Image
            src="/images/hero/floating-book.svg"
            alt=""
            fill
            className="object-contain"
            priority
            sizes="(max-width: 640px) 78vw, (max-width: 1024px) 46vw, 470px"
          />
        </div>
      </motion.div>
    </motion.div>
  );
}
