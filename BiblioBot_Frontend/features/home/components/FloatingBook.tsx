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
            : { translateY: [0, -12, 0], rotateZ: [-1, 1.2, -1] }
        }
        transition={{
          duration: 7.2,
          ease: "easeInOut",
          repeat: Infinity,
        }}
      >
        <div className="absolute inset-0 rounded-[2rem] bg-[radial-gradient(circle_at_45%_28%,rgba(227,192,113,0.32),transparent_36%),radial-gradient(circle_at_60%_72%,rgba(239,226,200,0.16),transparent_34%)] blur-2xl" />
        <div className="absolute bottom-4 left-1/2 h-24 w-[82%] -translate-x-1/2 rounded-full bg-black/45 blur-2xl" />
        <div className="absolute left-[11%] top-[9%] h-[78%] w-[13%] rounded-l-xl bg-[linear-gradient(90deg,rgba(239,226,200,0.18),rgba(239,226,200,0.03))] shadow-[inset_-10px_0_18px_rgba(0,0,0,0.22)]" />
        <div className="relative h-full w-full rounded-[1.8rem] border border-paper/12 bg-[linear-gradient(145deg,rgba(239,226,200,0.14),rgba(58,38,26,0.1))] p-4 shadow-[0_44px_84px_rgba(0,0,0,0.48)] backdrop-blur-sm">
          <Image
            src="/images/hero/floating-book.svg"
            alt=""
            fill
            className="object-contain opacity-95 drop-shadow-[0_28px_38px_rgba(0,0,0,0.35)] [filter:saturate(.82)_contrast(1.04)]"
            priority
            sizes="(max-width: 640px) 72vw, (max-width: 1024px) 46vw, 470px"
          />
          <div className="absolute inset-4 rounded-[1.4rem] bg-[linear-gradient(110deg,rgba(255,255,255,0.2),transparent_28%,transparent_68%,rgba(0,0,0,0.2))]" />
        </div>
      </motion.div>
    </motion.div>
  );
}
