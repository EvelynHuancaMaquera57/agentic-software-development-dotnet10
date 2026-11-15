"""Interfaz web con Streamlit para la calculadora de src/calculator.py.

Ejecutar desde la carpeta proyecto1/:
    streamlit run app.py
"""

import streamlit as st

from src.calculator import add, divide, multiply, subtract

# Etiqueta del botón -> (función, símbolo que se muestra en la operación)
OPERACIONES = {
    "➕ Sumar": (add, "+"),
    "➖ Restar": (subtract, "−"),
    "✖️ Multiplicar": (multiply, "×"),
    "➗ Dividir": (divide, "÷"),
}


def formatear(numero: float) -> str:
    """Muestra 5.0 como '5' y recorta decimales largos (0.1 + 0.2 -> '0.3')."""
    if float(numero).is_integer():
        return str(int(numero))
    return f"{numero:.10g}"


st.set_page_config(page_title="Calculadora", page_icon="🧮", layout="centered")
st.title("🧮 Calculadora")

# 1. Dos campos numéricos
col_a, col_b = st.columns(2)
with col_a:
    a = st.number_input("Primer número", value=0.0, step=1.0)
with col_b:
    b = st.number_input("Segundo número", value=0.0, step=1.0)

# 2. Un botón por operación
columnas = st.columns(len(OPERACIONES))
operacion_elegida = None
for columna, etiqueta in zip(columnas, OPERACIONES):
    if columna.button(etiqueta, width="stretch"):
        operacion_elegida = etiqueta

# 3. Resultado en grande / 4. Error de división por cero con mensaje amigable
if operacion_elegida:
    funcion, simbolo = OPERACIONES[operacion_elegida]
    try:
        resultado = funcion(a, b)
    except ValueError:
        st.warning(
            "😅 **No se puede dividir entre cero.** "
            "Cambia el segundo número por uno distinto de 0 e inténtalo de nuevo."
        )
    else:
        st.caption(f"{formatear(a)} {simbolo} {formatear(b)}")
        st.markdown(
            f"<p style='font-size:4rem;font-weight:700;margin:0'>= {formatear(resultado)}</p>",
            unsafe_allow_html=True,
        )
