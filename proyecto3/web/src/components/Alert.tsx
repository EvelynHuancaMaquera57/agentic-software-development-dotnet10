import type { ReactNode } from "react";

interface AlertProps {
  tone: "error" | "info" | "success";
  children: ReactNode;
}

/** Mensaje destacado. Los errores usan role="alert" para que se anuncien al aparecer. */
export function Alert({ tone, children }: AlertProps) {
  return (
    <div className={`alert alert--${tone}`} role={tone === "error" ? "alert" : "status"}>
      {children}
    </div>
  );
}
