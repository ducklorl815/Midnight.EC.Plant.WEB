package com.midnight.ec.plant;

import android.content.Intent;
import android.net.Uri;
import android.os.Handler;
import android.os.Looper;
import androidx.core.content.FileProvider;
import com.getcapacitor.Plugin;
import com.getcapacitor.PluginCall;
import com.getcapacitor.PluginMethod;
import com.getcapacitor.annotation.CapacitorPlugin;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.net.HttpURLConnection;
import java.net.URL;

@CapacitorPlugin(name = "ApkUpdate")
public class ApkUpdatePlugin extends Plugin {
    @PluginMethod
    public void install(PluginCall call) {
        String url = call.getString("url");
        if (url == null || url.isEmpty()) {
            call.reject("缺少安裝檔網址");
            return;
        }
        new Thread(() -> {
            try {
                File file = download(url);
                new Handler(Looper.getMainLooper()).post(() -> {
                    try {
                        openInstaller(file);
                        call.resolve();
                    } catch (Exception ex) {
                        call.reject(ex.getMessage() == null ? "無法開啟安裝程式" : ex.getMessage());
                    }
                });
            } catch (Exception ex) {
                new Handler(Looper.getMainLooper()).post(() ->
                    call.reject(ex.getMessage() == null ? "下載安裝檔失敗" : ex.getMessage()));
            }
        }).start();
    }

    private File download(String url) throws Exception {
        HttpURLConnection connection = (HttpURLConnection) new URL(url).openConnection();
        connection.setInstanceFollowRedirects(true);
        connection.connect();
        if (connection.getResponseCode() >= 400) {
            throw new IllegalStateException("下載安裝檔失敗：" + connection.getResponseCode());
        }
        File file = new File(getContext().getCacheDir(), "plant-update.apk");
        try (InputStream input = connection.getInputStream();
             FileOutputStream output = new FileOutputStream(file)) {
            byte[] buffer = new byte[8192];
            int read;
            while ((read = input.read(buffer)) != -1) {
                output.write(buffer, 0, read);
            }
        } finally {
            connection.disconnect();
        }
        return file;
    }

    private void openInstaller(File file) {
        Uri uri = FileProvider.getUriForFile(
            getContext(),
            getContext().getPackageName() + ".fileprovider",
            file);
        Intent intent = new Intent(Intent.ACTION_VIEW);
        intent.setDataAndType(uri, "application/vnd.android.package-archive");
        intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_ACTIVITY_NEW_TASK);
        getContext().startActivity(intent);
    }
}
