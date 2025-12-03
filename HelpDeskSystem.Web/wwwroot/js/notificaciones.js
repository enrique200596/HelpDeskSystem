// wwwroot/js/notificaciones.js
function mostrarNotificacion(mensaje, tipo) {
    // Crear el contenedor de toasts si no existe
    let contenedor = document.getElementById('toast-container');
    if (!contenedor) {
        contenedor = document.createElement('div');
        contenedor.id = 'toast-container';
        document.body.appendChild(contenedor);
    }

    // Crear el elemento del toast
    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${tipo}`;
    toast.textContent = mensaje;

    // Añadir icono
    const icono = document.createElement('i');
    icono.className = tipo === 'success' ? 'bi bi-check-circle-fill' : 'bi bi-x-circle-fill';
    toast.prepend(icono);

    // Añadir al contenedor
    contenedor.appendChild(toast);

    // Mostrar con animación
    setTimeout(() => {
        toast.classList.add('show');
    }, 100);

    // Ocultar y eliminar después de 5 segundos
    setTimeout(() => {
        toast.classList.remove('show');
        toast.addEventListener('transitionend', () => {
            toast.remove();
            // Si el contenedor queda vacío, lo podemos eliminar
            if (contenedor.childElementCount === 0) {
                contenedor.remove();
            }
        });
    }, 5000);
}
