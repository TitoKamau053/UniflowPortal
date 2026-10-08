(function () {
    let initialized = false;

    function initLoginVideo() {
        if (initialized) return;

        const videoA = document.getElementById("loginVideoA");
        const videoB = document.getElementById("loginVideoB");

        if (!videoA || !videoB) return;

        initialized = true;

        let active = videoA;
        let standby = videoB;
        let transitioning = false;

        videoA.muted = true;
        videoB.muted = true;

        videoA.play().catch(() => {});

        function transition() {
            if (transitioning) return;

            transitioning = true;

            standby.currentTime = 0;

            standby.play().catch(() => {});

            standby.classList.remove("video-hidden");
            standby.classList.add("video-visible");

            active.classList.remove("video-visible");
            active.classList.add("video-hidden");

            const oldActive = active;

            active = standby;
            standby = oldActive;

            setTimeout(() => {
                standby.pause();
                standby.currentTime = 0;
                transitioning = false;
            }, 1200);
        }

        function monitor(video) {
            video.addEventListener("timeupdate", function () {
                if (!video.duration || transitioning) return;

                const remaining = video.duration - video.currentTime;

                if (remaining <= 1.2) {
                    transition();
                }
            });
        }

        monitor(videoA);
        monitor(videoB);

        videoA.addEventListener("ended", () => {
            if (!transitioning) transition();
        });

        videoB.addEventListener("ended", () => {
            if (!transitioning) transition();
        });
    }

    window.initLoginVideo = initLoginVideo;
})();