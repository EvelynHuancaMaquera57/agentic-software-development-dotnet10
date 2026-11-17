import Icon from './Icon'
import styles from './Button.module.css'

/**
 * Botón/enlace reutilizable.
 * variant: 'primary' | 'secondary' | 'white'
 * size:    'md' | 'lg'
 * onDark:  ajusta el estilo secundario para fondos oscuros
 */
export default function Button({
  href,
  children,
  variant = 'primary',
  size = 'md',
  onDark = false,
  iconRight,
  className = '',
  ...rest
}) {
  const classes = [
    styles.button,
    styles[variant],
    styles[size],
    onDark && styles.onDark,
    className,
  ]
    .filter(Boolean)
    .join(' ')

  const content = (
    <>
      <span>{children}</span>
      {iconRight && <Icon name={iconRight} size={18} className={styles.icon} />}
    </>
  )

  if (href) {
    return (
      <a href={href} className={classes} {...rest}>
        {content}
      </a>
    )
  }
  return (
    <button type="button" className={classes} {...rest}>
      {content}
    </button>
  )
}
