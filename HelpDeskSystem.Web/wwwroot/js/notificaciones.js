function reproducirSonido() {
    // Asegúrate de poner un archivo 'alert.mp3' en wwwroot/sounds/
    try {
        var audio = new Audio('/sounds/alert.mp3');
        audio.play().catch(e => console.log("Audio bloqueado por navegador hasta interacción"));
    } catch (e) { }
}

function mostrarNotificacion(mensaje, tipo) {
    reproducirSonido(); // <--- SONIDO

    let contenedor = document.getElementById('toast-container');
    if (!contenedor) {
        contenedor = document.createElement('div');
        contenedor.id = 'toast-container';
        document.body.appendChild(contenedor);
    }

    const toast = document.createElement('div');
    // Agregamos clase 'slide-in' para animaciones CSS
    toast.className = `toast-notification toast-${tipo} slide-in`;

    // HTML más agresivo para alertas
    toast.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="bi bi-bell-fill fs-2 me-3"></i>
            <div>
                <strong class="d-block text-uppercase" style="font-size:1.1rem;">NUEVA ACTIVIDAD</strong>
                <span class="fw-bold">${mensaje}</span>
            </div>
        </div>
    `;

    contenedor.appendChild(toast);

    setTimeout(() => { toast.classList.add('show'); }, 100);

    // Duración de 6 segundos
    setTimeout(() => {
        toast.classList.remove('show');
        toast.addEventListener('transitionend', () => toast.remove());
    }, 6000);
}