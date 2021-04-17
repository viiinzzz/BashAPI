
//pdf.js
var pdfLib = window["pdfjs-dist/build/pdf"];
pdfLib.GlobalWorkerOptions.workerSrc = "pdf.worker.js";

var pdfUrl = null,
    pdfDoc = null,
    pdfPageCurrent = 1,
    pdfPageRendering = false,
    pdfPagePending = null,
    pdfScale = 1,
    pdfScaleCoef = 1.1,
    pdfCanvas = null,
    pdfCanvasContext = null;


jQuery(document).ready(function () {
    if (window.File
        && window.FileReader
        && window.FileList
        && window.Blob)
    {
        pdfCanvas = $("#pdf-layer")[0];
        pdfCanvasContext = pdfCanvas.getContext("2d");
        envir();
        $("#file").change(event => feat());
        $(window).resize(debouncer(function (e) {
            pdfRenderQueue(pdfPageCurrent);
        }));
        $("#zoomDrop").text("100%");
        $(`[id^="zoomX"]`).each(function () {
            var z = parseInt($(this).attr("id").replace(/zoomX(.*)/gm, "$1"));
            $(this).text(`${z}%`);
            $(this).click(() => {
                $("#zoomDrop").text(`${z}%`);
                pdfScale = z * 0.01;
                pdfRenderQueue(pdfPageCurrent);
            });
        });
        $("#zoom-").click(() => {
            pdfScale /= pdfScaleCoef;
            var z = Math.round(pdfScale * 100);
            $("#zoomDrop").text(`${z}%`);
            pdfRenderQueue(pdfPageCurrent);
        });
        $("#zoom\\+").click(() => {
            pdfScale *= pdfScaleCoef;
            var z = Math.round(pdfScale * 100);
            $("#zoomDrop").text(`${z}%`);
            pdfRenderQueue(pdfPageCurrent);
        });
        $("#save").click(() => pdfSave());
        $("#fullscreen").click(() => {
            if (pdfDoc?.numPages >= 1) {
                if ($("#top").css("display") == "none")
                    $("#top").css("display", "block");
                else
                    $("#top").css("display", "none");
            }
        });
    }
    else {
        $("#form").css("display", "none");
        $("#inner").text("The File APIs are not fully supported in this browser.");
    }
});

function envir() {
    $.ajax({//display environment Development / Staging / Production
        url: `${window.location.protocol}//${window.location.host}/envir`,
        contentType: "text/plain",
        //dataType: 'json',
        success: function (envir) {
            $("#envir").css("display", envir == "Production" ? "none" : "block");
            $("#envir").text(envir);
        }
    });
}

function feat() {
    const endpoint = `${window.location.protocol}//${window.location.host}/feat`;
    const inputfile = $('#file')[0].files[0];
    const data = new FormData(); data.append("file", inputfile);

    $("#filelabel").text(inputfile.name);
    $("#toolbar").css("display", "none");
    pdfCanvasContext.clear();
    $("#pdf-layer").css("display", "none");
    $("#failed").css("display", "none");
    $("#spin").css("display", "block");

    //new Response(data).text().then(console.log);
    fetch(endpoint, { method: "POST", body: data })
        .then(res => {
            if (res.status !== 200) {
                console.log(`${endpoint} Reponse status=${res.status}`);
                $("#failed").css("display", "block");
            } else return res.blob();
        })
        .then(blob => {
            if (blob) {
                pdfUrl = window.URL.createObjectURL(blob);
                console.log(`${endpoint} blob url=${pdfUrl}`);
                $("#toolbar").css("display", "block");
                $("#pdf-layer").css("display", "block");
                pdfLoad();
            } else {
                console.log(`${endpoint} no blob`);
                $("#failed").css("display", "block");
            }
            $("#spin").css("display", "none");

        })
        .catch(err => {
            console.log(`${endpoint} error=${err}`);
            $("#failed").css("display", "block");
        });
}


function pdfLoad() {
    pdfLib.getDocument(pdfUrl).promise.then(function (pdfDoc_) {
        pdfDoc = pdfDoc_;
        const pages_i = Array.from({ length: pdfDoc.numPages }, (_, i) => i + 1)
        const begin = `<nav aria-label="Pages"><ul class="pagination m-0 justify-content-center">
            <li class="page-item disabled"><a role="button" class="prev page-link" href="#" tabindex="-1"><div class="vflip">➜</div></a></li>`;
        const end = `<li class="page-item"><a role="button" class="next page-link" href="#">➜</a></li>
            </ul></nav>`;
        const middle = pages_i
            .map(i => `<li class="page-item"><a role="button" class="page${i} page-link" href="#">${i}</a></li>`)
            .reduce((x, y) => `${x}${'\n'}${y}`);
        $(".pages").html(`${begin}${'\n'}${middle}${'\n'}${end}`);


        $(".prev").click(() => {
            if (pdfPageCurrent <= 1) return;
            pdfPageCurrent--;
            pdfRenderQueue(pdfPageCurrent);
        });

        $(".next").click(() => {
            if (pdfPageCurrent >= pdfDoc.numPages) return;
            pdfPageCurrent++;
            pdfRenderQueue(pdfPageCurrent);
        });

        pages_i.forEach(i => $(`.page${i}`).click(() => {
            pdfPageCurrent = i;
            pdfRenderQueue(pdfPageCurrent);
        }));

        pdfRender(pdfPageCurrent);
    });
}


function pdfRender(num) {
    if (num < 1 || num > pdfDoc.numPages) return;

    pdfPageRendering = true;
    // Using promise to fetch the page
    pdfDoc.getPage(num).then(page => {
        const w = $("#inner").width();
        //const h = ...;
        const vp1 = page.getViewport({ scale: 1 });
        //const s = Math.min(h / vp1.height, w / vp1.width);
        const s = w / vp1.width;
        var viewport = page.getViewport({ scale: s * pdfScale});
        pdfCanvas.height = viewport.height;
        pdfCanvas.width = viewport.width;

        // Render PDF page into canvas context
        var renderContext = {
            canvasContext: pdfCanvasContext,
            viewport: viewport
        };
        var renderTask = page.render(renderContext);

        // Wait for rendering to finish
        renderTask.promise.then(function () {
            $("#text-layer").height($("#pdf-layer").height());
            pdfPageRendering = false;
            if (pdfPagePending !== null) {
                // New page rendering is pending
                pdfRender(pdfPagePending);
                pdfPagePending = null;
            }
        }).then(function () {
            // Returns a promise, on resolving it will return text contents of the page
            return page.getTextContent();
        }).then(function (textContent) {
            var canvas_offset = $("#pdf-layer").offset();
            var canvas_height = $("#pdf-layer")[0].height;
            var canvas_width = $("#pdf-layer")[0].width;

            $("#text-layer").css({ left: canvas_offset.left + 'px', top: canvas_offset.top + 'px', height: canvas_height + 'px', width: canvas_width + 'px' });

            // Pass the data to the method for rendering of text over the pdf canvas.
            pdfjsLib.renderTextLayer({
                scale: s * pdfScale,
                textContent: textContent,
                container: $("#text-layer").get(0),
                viewport: viewport,
                textDivs: []
            });
        });
    });

    for (i = 1; i <= pdfDoc.numPages; i++)
        if (i == num)
            $(`.page${i}`).parent().addClass("active");
        else
            $(`.page${i}`).parent().removeClass("active");
    if (num == 1)
        $(".prev").parent().addClass("disabled");
    else
        $(".prev").parent().removeClass("disabled");
    if (num == pdfDoc.numPages)
        $(".next").parent().addClass("disabled");
    else
        $(".next").parent().removeClass("disabled");
}


//If another page rendering in progress, waits until the rendering is finised. Otherwise, executes rendering immediately.
function pdfRenderQueue(num) {
    if (pdfPageRendering)  pdfPagePending = num;
    else pdfRender(num);
}

function pdfSave() {
    if (pdfUrl == null) return;
    const name = $('#file')[0].files[0].name
        .replace(/(.*).zip/igm, "$1")
        .replace(/(.*).ly/igm, "$1");
    var a = document.createElement("a");
    document.body.appendChild(a);
    a.style = "display: none";
    a.href = pdfUrl;
    a.download = name;
    a.click();
    window.URL.revokeObjectURL(url);
}




/*
 * helpers
 */

function debouncer(func, timeout) {
    var timeoutID, timeout = timeout || 500;
    return function () {
        var scope = this, args = arguments;
        clearTimeout(timeoutID);
        timeoutID = setTimeout(function () {
            func.apply(scope, Array.prototype.slice.call(args));
        }, timeout);
    }
}


CanvasRenderingContext2D.prototype.clear =
    CanvasRenderingContext2D.prototype.clear || function (preserveTransform) {
        if (preserveTransform) {
            this.save();
            this.setTransform(1, 0, 0, 1, 0, 0);
        }

        this.clearRect(0, 0, this.canvas.width, this.canvas.height);

        if (preserveTransform) {
            this.restore();
        }
    };