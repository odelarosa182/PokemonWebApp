const searchBox = document.getElementById("searchBox");
const pokemonList = document.getElementById("pokemonList");
const speciesSelect = document.getElementById("speciesSelect");
const loadingSpinner = document.getElementById("loadingSpinner");
let timeout = null;

// 🔁 Cargar especies al cargar la vista
window.addEventListener("DOMContentLoaded", () => {
    fetch("/Pokemon/GetSpeciesList")
        .then(res => res.json())
        .then(data => {
            data.forEach(s => {
                const option = document.createElement("option");
                option.value = s.name;
                option.textContent = `${s.name}`;
                speciesSelect.appendChild(option);
            });
        });
});

// 🔎 Función para filtrar
function loadFilteredPokemon(page = 1) {
    const name = searchBox.value.trim();
    const species = speciesSelect.value;

    loadingSpinner.classList.remove("d-none");

    fetch(`/Pokemon/Filter?name=${encodeURIComponent(name)}&species=${encodeURIComponent(species)}&page=${page}`)
        .then(res => res.text())
        .then(html => {
            pokemonList.innerHTML = html;
        })
        .catch(err => {
            pokemonList.innerHTML = "<p class='text-danger text-center'>Error al filtrar Pokémon.</p>";
            console.error(err);
        })
        .finally(() => {
            loadingSpinner.classList.add("d-none");
        });
}

// 🧠 Eventos
searchBox.addEventListener("keyup", () => {
    clearTimeout(timeout);
    timeout = setTimeout(() => loadFilteredPokemon(1), 300);
});

speciesSelect.addEventListener("change", () => loadFilteredPokemon(1));

// 🔁 Paginación con filtro activo
document.addEventListener("click", function (e) {
    if (e.target.classList.contains("pagination-link")) {
        e.preventDefault();
        const page = e.target.dataset.page;
        loadFilteredPokemon(page);
    }
});
// 📥 Exportar a Excel
document.getElementById("btnExportExcel").addEventListener("click", async function () {
    const name = document.getElementById("searchBox").value.trim();
    const species = document.getElementById("speciesSelect").value.trim();
    const spinner = document.getElementById("exportSpinner");

    spinner.classList.remove("d-none"); // mostrar spinner

    try {
        // Validar si hay resultados antes de descargar
        const res = await fetch(`/Pokemon/CountFiltered?name=${encodeURIComponent(name)}&species=${encodeURIComponent(species)}`);
        const data = await res.json();

        if (data.count > 0) {
            const url = `/Pokemon/ExportToExcelFiltered?name=${encodeURIComponent(name)}&species=${encodeURIComponent(species)}`;
            window.location.href = url;
        } else {
            alert("No hay resultados para exportar con los filtros aplicados.");
        }
    } catch (err) {
        alert("Ocurrió un error al intentar exportar.");
        console.error(err);
    } finally {
        setTimeout(() => {
            spinner.classList.add("d-none"); // ocultar spinner con retardo
        }, 1000); // opcional: espera un segundo para evitar parpadeo
    }

    
});

document.getElementById("emailForm").addEventListener("submit", async function (e) {
    e.preventDefault();

    const email = document.getElementById("emailInput").value.trim();
    const name = document.getElementById("searchBox").value.trim();
    const species = document.getElementById("speciesSelect").value.trim();
    const status = document.getElementById("emailStatus");

    if (!email) {
        alert("Por favor escribe un correo válido.");
        return;
    }

    status.classList.remove("d-none");
    status.textContent = "Enviando correo...";

    try {
        const res = await fetch("/Pokemon/SendReportByEmail", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ email, name, species })
        });

        const result = await res.json();

        if (result.success) {
            status.textContent = "Correo enviado correctamente.";
        } else {
            status.textContent = "Error al enviar correo.";
        }
    } catch (err) {
        console.error(err);
        status.textContent = "Error inesperado.";
    }

    setTimeout(() => {
        const modal = bootstrap.Modal.getInstance(document.getElementById("emailModal"));
        modal.hide();
        status.classList.add("d-none");
        status.textContent = "";
    }, 3000);
});

document.addEventListener("click", async function (e) {
    const target = e.target.closest(".view-details");

    if (target) {
        e.preventDefault();

        const name = target.getAttribute("data-name");
        const contentDiv = document.getElementById("pokemonModalContent");
        const modal = new bootstrap.Modal(document.getElementById("pokemonModal"));

        contentDiv.innerHTML = "<p>Cargando...</p>";
        modal.show();

        try {
            const res = await fetch(`/Pokemon/GetDetails?name=${encodeURIComponent(name)}`);
            const data = await res.json();

            const abilities = data.abilities.map(a => `<li>${a.name}${a.isHidden ? ' (oculta)' : ''}</li>`).join('');
            const types = data.types.map(t => `<span class="badge bg-secondary me-1">${t}</span>`).join('');

            contentDiv.innerHTML = `
                <img src="${data.image}" alt="${name}" class="img-fluid mb-3" style="max-height: 150px;" />
                <p><strong>ID:</strong> ${data.id}</p>
                <p><strong>Tipos:</strong><br> ${types}</p>
                <p><strong>Habilidades:</strong></p>
                <ul>${abilities}</ul>
            `;
        } catch (err) {
            contentDiv.innerHTML = `<p class="text-danger">Error al cargar los detalles.</p>`;
        }
    }
});