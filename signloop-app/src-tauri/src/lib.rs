use tauri::command;

// Command invoked from React
#[command]
fn run_model_inference(coordinates: Vec<f32>) -> String {
    // 1. Verify tensor size (30 frames * 225 dimensions = 6750 floats)
    if coordinates.len() != 30 * 225 {
        return "Invalid tensor dimensions".into();
    }

    // 2. Here is where you load your exported ONNX model session and run the forward pass:
    // let session = ort::Session::builder()?.commit_from_file("path/to/signloop_model.onnx")?;
    // let input_tensor = ndarray::Array::from_shape_vec((1, 30, 225), coordinates)?;
    // let outputs = session.run(ort::inputs![input_tensor]?)?;

    // 3. For now, we return a mock translation string to establish the pipeline loop
    "Hello from SignLoop RL Backend!".into()
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        // Register the command here so Tauri exposes it to the webview
        .invoke_handler(tauri::generate_handler![run_model_inference])
        // FIX 1: Added the exclamation mark to invoke the macro
        .run(tauri::generate_context!())
        // FIX 2: Swapped to standard Result handling
        .expect("error while running tauri application");
}